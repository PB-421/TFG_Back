using Microsoft.AspNetCore.Mvc;
using Supabase;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly Client _client;

    public AuthController(Client client)
    {
        _client = client;
    }

    // ---------------- REGISTER ----------------
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email y password son requeridos");

        var session = await _client.Auth.SignUp(request.Email, request.Password);

        if (session?.User == null)
            return BadRequest("No se pudo crear el usuario");

        var profile = new Profile
        {
            Id = Guid.Parse(session.User.Id!),
            Email = request.Email,
            Name = request.Name ?? "",
            Role = "student"
        };

        await _client.From<Profile>().Insert(profile);

        return Ok("Usuario registrado correctamente");
    }
    [HttpPost("profile")]
    public async Task<IActionResult> GetProfile([FromBody] LoginDto data)
    {
        Console.WriteLine($"ID: {data.Id}, Email: {data.Email}, Name: {data.Name}");

        if (string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Id))
            return BadRequest("Faltan datos");

        // Buscar si ya existe
        var result = await _client.From<Profile>()
                                .Where(p => p.Email == data.Email)
                                .Select("*")
                                .Get();

        var profile = result.Models.FirstOrDefault();

        if (profile == null)
        {
            profile = new Profile
            {
                Id = Guid.Parse(data.Id),
                Email = data.Email,
                Name = data.Name,
                Role = "student"
            };

            await _client.From<Profile>().Insert(profile);
        }

        return Ok("Bienvenido "+data.Name);
    }
    // ---------------- LOGIN ----------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var session = await _client.Auth.SignIn(request.Email, request.Password);
        if (session == null)
            return Unauthorized("Credenciales incorrectas");

        SetAuthCookies(session);

        var userId = Guid.Parse(session.User!.Id!);

        var result = await _client
            .From<Profile>()
            .Where(p => p.Id == userId)
            .Single();

        return Ok(new
        {
            Message = $"Bienvenido {result.Name}",
            result.Name,
            result.Role
        });
    }

    // ---------------- AUTO LOGIN ----------------
    [HttpGet("auto-login")]
    public async Task<IActionResult> AutoLogin()
    {
        if (!Request.Cookies.TryGetValue("sb-refresh-token", out var refreshToken))
            return Unauthorized("No hay sesión activa");

        Request.Cookies.TryGetValue("sb-access-token", out var accessToken);

        try
        {
            await _client.Auth.SetSession(accessToken!, refreshToken);

            var user = _client.Auth.CurrentUser;
            if (user == null)
                return Unauthorized("Sesión inválida");

            var userId = Guid.Parse(user.Id!);

            var profile = await _client
                .From<Profile>()
                .Where(p => p.Id == userId)
                .Single();

            return Ok(new
            {
                Message = $"Hola de nuevo {profile.Name}",
                profile.Name,
                profile.Role
            });
        }
        catch
        {
            return Unauthorized("Sesión expirada");
        }
    }

    // ---------------- LOGOUT ----------------
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await _client.Auth.SignOut();

        Response.Cookies.Delete("sb-access-token");
        Response.Cookies.Delete("sb-refresh-token");

        return Ok("Logout OK");
    }

    // ---------------- HELPERS ----------------
    private void SetAuthCookies(Supabase.Gotrue.Session session)
    {
        Response.Cookies.Append(
            "sb-access-token",
            session.AccessToken!,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true en producción
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn)
            });

        Response.Cookies.Append(
            "sb-refresh-token",
            session.RefreshToken!,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
    }
}
