using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetEnv;

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

    // ---------------- LOGIN MICROSOFT ----------------
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
                Name = data.Name!,
                Role = "student"
            };

            await _client.From<Profile>().Insert(profile);
        }

        return Ok(new { 
        name = profile.Name, 
        role = profile.Role 
        });
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
            Message = $"Bienvenido {result!.Name}",
            result.Name,
            result.Role
        });
    }

    // ---------------- AUTO LOGIN ----------------
    [HttpGet("auto-login")]
    public async Task<IActionResult> AutoLogin()
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("No hay sesión activa");

        var http = new HttpClient();

        http.DefaultRequestHeaders.Add("apikey", Environment.GetEnvironmentVariable("DB_KEY")!);

        var body = new
        {
            refresh_token = refreshToken
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );
        Console.WriteLine(refreshToken);
        var response = await http.PostAsync(
            Environment.GetEnvironmentVariable("DB_URL")!+"/auth/v1/token?grant_type=refresh_token",
            content
        );

        if (!response.IsSuccessStatusCode)
            return Unauthorized("Refresh token inválido");

        var json = await response.Content.ReadAsStringAsync();

        var session = JsonSerializer.Deserialize<SupabaseRefreshResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (session == null || session.user == null)
            return Unauthorized("Sesión inválida");

        // 🔐 Guardar nuevos tokens en cookies
        SetAutoAuthCookies(session.access_token, session.refresh_token, session.expires_in);

        // Obtener perfil
        var userId = Guid.Parse(session.user.id);

        var result = await _client
            .From<Profile>()
            .Where(p => p.Id == userId)
            .Single();

        return Ok(new
        {
            Message = $"Hola de nuevo {result!.Name}",
            result.Name,
            result.Role
        });
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
        var accessExp = DateTimeOffset.UtcNow.AddSeconds(5);
        var refreshExp = DateTimeOffset.UtcNow.AddDays(30);

        Response.Cookies.Append(
            "sb-access-token",
            session.AccessToken!,   // <-- añadimos fecha
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = accessExp
            });

        Response.Cookies.Append(
            "sb-refresh-token",
            session.RefreshToken!,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = refreshExp
            });
    }

    private void SetAutoAuthCookies(string accessToken, string refreshToken, int expiresIn)
    {
        var accessExp = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        var refreshExp = DateTimeOffset.UtcNow.AddDays(30);

        Response.Cookies.Append(
            "sb-access-token",
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax, //Cambiar a None y true en prod
                Expires = accessExp
            });

        Response.Cookies.Append(
            "sb-refresh-token",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = refreshExp
            });
    }

}
