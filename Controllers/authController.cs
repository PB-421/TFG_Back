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
            try 
            {
                var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(ex.Message);
                var mensajeLimpio = errorData?["msg"]?.ToString() ?? "Error desconocido";
                return BadRequest(new { error = mensajeLimpio });
            }
            catch 
            {
                return BadRequest(new { error = ex.Message });
            }
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
            try 
            {
                var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(ex.Message);
                var mensajeLimpio = errorData?["msg"]?.ToString() ?? "Error desconocido";
                return BadRequest(new { error = mensajeLimpio });
            }
            catch 
            {
                return BadRequest(new { error = ex.Message });
            }
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
            try 
            {
                var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(ex.Message);
                var mensajeLimpio = errorData?["msg"]?.ToString() ?? "Error desconocido";
                return BadRequest(new { error = mensajeLimpio });
            }
            catch 
            {
                return BadRequest(new { error = ex.Message });
            }
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
            try 
            {
                var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(ex.Message);
                var mensajeLimpio = errorData?["msg"]?.ToString() ?? "Error desconocido";
                return BadRequest(new { error = mensajeLimpio });
            }
            catch 
            {
                return BadRequest(new { error = ex.Message });
            }
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
            try 
            {
                var errorData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(ex.Message);
                var mensajeLimpio = errorData?["msg"]?.ToString() ?? "Error desconocido";
                return BadRequest(new { error = mensajeLimpio });
            }
            catch 
            {
                return BadRequest(new { error = ex.Message });
            }
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
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = refreshExp
            });
    }

}
