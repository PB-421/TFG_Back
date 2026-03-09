using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _authService;

    private readonly IProfilesAppService _profileService;

    public AuthController(IAuthAppService authService, IProfilesAppService profilesService)
    {
        _authService = authService;
        _profileService = profilesService;
    }

    // ---------------- REGISTER ----------------
    [HttpPost("register")]
    public async Task<IActionResult> Register(LoginDto request)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Usuario no autorizado");

        var currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);

        if (currentUser == null || currentUser.Role != "admin")
            return Unauthorized("Usuario no autorizado");
        
        var (session, error) = await _authService.RegisterAsync(request);

        if (error != null)
            return BadRequest(error);

        return Ok("Usuario registrado correctamente");
    }

    // ---------------- LOGIN ----------------
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var (session, profile, error) = await _authService.LoginAsync(request);

        if (error != null)
            return Unauthorized(error);

        SetAuthCookies(session!);

        return Ok(new
        {
            Message = $"Bienvenido {profile!.Name}",
            profile.Name,
            profile.Role
        });
    }

    // ---------------- AUTO LOGIN ----------------
    [HttpGet("auto-login")]
    public async Task<IActionResult> AutoLogin()
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        var result = await _authService.RefreshSessionAsync(refreshToken!);

        if (result == null)
            return Unauthorized("Sesión expirada");

        var (accessToken, newRefreshToken, expiresIn, profile) = result.Value;

        SetAutoAuthCookies(accessToken, newRefreshToken, expiresIn);

        return Ok(new
        {
            Message = $"Hola de nuevo {profile!.Name}",
            profile.Name,
            profile.Role
        });
    }

    // ---------------- OAUTH PROFILE ----------------
    [HttpPost("profile")]
    public async Task<IActionResult> Profile(LoginDto data)
    {
        var profile = await _authService.GetOrCreateOAuthProfile(data);

        if (profile == null)
            return BadRequest();

        return Ok(new
        {
            name = profile.Name,
            role = profile.Role
        });
    }

    // ---------------- LOGOUT ----------------
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();

        Response.Cookies.Delete("sb-access-token");
        Response.Cookies.Delete("sb-refresh-token");

        return Ok("Logout OK");
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Usuario no autorizado");

        var currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);

        if (currentUser == null || currentUser.Role != "admin")
            return Unauthorized("Usuario no autorizado");

        var result = await _authService.DeleteUserAsync(id);

        if (!result)
            return BadRequest("No se pudo eliminar el usuario");

        return Ok("Usuario eliminado");
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
