using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    public async Task<IActionResult> Register([FromBody] LoginDto request,[FromQuery] Guid adminId)
    {
        try{
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
                return Unauthorized();
            var currentUser = new Profile();
            if(!string.IsNullOrEmpty(refreshToken)){
                currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);
            } else
            {
                currentUser = await _profileService.GetCurrentUserProfileAsync(adminId);
            }

            if (currentUser == null || currentUser.Role != "admin")
                return Unauthorized("Usuario no autorizado");
            
            var (session, error) = await _authService.RegisterAsync(request);

            if (error != null)
                return BadRequest(error);

            return Ok("Usuario registrado correctamente");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ocurrió un error inesperado: "+ex);
        }
    }

    // ---------------- LOGIN ----------------
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        try{
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
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ocurrió un error inesperado: "+ex);
        }
    }

    // ---------------- AUTO LOGIN ----------------
    [HttpGet("auto-login")]
    public async Task<IActionResult> AutoLogin()
    {
        try{
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
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ocurrió un error inesperado: "+ex);
        }
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
        try{
            await _authService.LogoutAsync();

            Response.Cookies.Delete("sb-access-token");
            Response.Cookies.Delete("sb-refresh-token");

            return Ok("Logout OK");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ocurrió un error inesperado: "+ex);
        }
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, [FromQuery] Guid adminId)
    {
        try{
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
                return Unauthorized();
            var currentUser = new Profile();
            if(!string.IsNullOrEmpty(refreshToken)){
                currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);
            } else
            {
                currentUser = await _profileService.GetCurrentUserProfileAsync(adminId);
            }
            if (currentUser == null || currentUser.Role != "admin")
                return Unauthorized("Usuario no autorizado");

            var result = await _authService.DeleteUserAsync(id);

            if (!result)
                return BadRequest("No se pudo eliminar el usuario");

            return Ok("Usuario eliminado");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ocurrió un error inesperado: "+ex);
        }
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
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = accessExp
            });

        Response.Cookies.Append(
            "sb-refresh-token",
            session.RefreshToken!,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
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
                Secure = true,
                SameSite = SameSiteMode.None, //Cambiar a None y true en prod
                Expires = accessExp
            });

        Response.Cookies.Append(
            "sb-refresh-token",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = refreshExp
            });
    }

    private string ExtractSupabaseMessage(Supabase.Gotrue.Exceptions.GotrueException ex)
    {
        if (ex == null || string.IsNullOrEmpty(ex.Message))
            return "Error desconocido de Supabase";

        try
        {
            var json = JsonSerializer.Deserialize<JsonObject>(ex.Message);
            if (json != null)
            {
                var msg = json["msg"]?.ToString();
                if (!string.IsNullOrEmpty(msg)) return msg;

                var error = json["error"]?.ToString();
                if (!string.IsNullOrEmpty(error)) return error;

                var description = json["error_description"]?.ToString();
                if (!string.IsNullOrEmpty(description)) return description;

                var messageField = json["message"]?.ToString();
                if (!string.IsNullOrEmpty(messageField)) return messageField;

                return json.ToString();
            }

            return ex.Message;
        }
        catch
        {
            return ex.Message;
        }
    }

    private IActionResult SupabaseErrorResponse(Supabase.Gotrue.Exceptions.GotrueException ex, int statusCode = 400)
    {
        var mensaje = ExtractSupabaseMessage(ex);
        var payload = new { error = mensaje };
        return StatusCode(statusCode, payload);
    }

}
