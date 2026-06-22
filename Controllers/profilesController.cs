using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IProfilesAppService _profilesService;
    private readonly ISubjectsAppService _subjectsService;

    public ProfilesController(IProfilesAppService profilesService, ISubjectsAppService subjectsService)
    {
        _profilesService = profilesService;
        _subjectsService = subjectsService;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll([FromQuery] Guid adminId)
    {
        try
        {
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
                return Unauthorized();

            var profiles = new List<Profile>();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                profiles = await _profilesService.GetAllProfilesAsync(refreshToken!);
            }
            else
            {
                profiles = await _profilesService.GetAllProfilesAsync(adminId);
            }

            if (profiles == null || profiles.Count == 0)
                return Unauthorized();

            var tasks = profiles.Select(async p => new profileDto
            {
                Id = p.Id,
                Email = p.Email,
                Name = p.Name,
                Role = p.Role,
                Subjects = await _subjectsService.GetSubjectNamesByIds(p.Subjects.ToList())
            });

            var dtoList = await Task.WhenAll(tasks);

            return Ok(dtoList);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("GetSession")]
    public async Task<IActionResult> GetSession()
    {
        try
        {
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("No hay sesión activa");

            var session = await _profilesService.GetCurrentSessionAsync(refreshToken);

            if (session == null)
                return Unauthorized("Sesión inválida o expirada");

            return Ok(session);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("GetUser")]
    public async Task<IActionResult> GetUser([FromQuery] Guid id)
    {
        try
        {
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken) && id == Guid.Empty)
                return Unauthorized();

            Profile currentUser;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                currentUser = await _profilesService.GetCurrentUserProfileAsync(refreshToken);
            }
            else
            {
                currentUser = await _profilesService.GetCurrentUserProfileAsync(id);
            }

            if (currentUser == null)
                return BadRequest("Usuario no encontrado");

            var User = new profileDto
            {
                Id = currentUser.Id,
                Email = currentUser.Email,
                Name = currentUser.Name,
                Role = currentUser.Role,
                Subjects = await _subjectsService.GetSubjectNamesByIds(currentUser.Subjects.ToList())
            };
            return Ok(User);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfileById(Guid id)
    {
        try
        {
            var profile = await _profilesService.GetProfileById(id);
            if (profile == null)
                return NotFound("Perfil no encontrado");

            return Ok(profile);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpPut("UpdateUserSubjects")]
    public async Task<IActionResult> UpdateUserSubjects([FromBody] List<Guid> newSubjectsIds, [FromQuery] Guid userId)
    {
        try
        {
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken) && userId == Guid.Empty)
                return Unauthorized();

            Profile currentUser;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                currentUser = await _profilesService.GetCurrentUserProfileAsync(refreshToken);
            }
            else
            {
                currentUser = await _profilesService.GetCurrentUserProfileAsync(userId);
            }

            if (currentUser == null || currentUser.Role != "student")
                return Unauthorized("Usuario no autorizado");

            var result = await _profilesService.UpdateProfileSubjects(newSubjectsIds, currentUser.Id);

            if (!result)
                return BadRequest("No hubo cambios o no autorizado");

            return Ok("Asignaturas actualizadas");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // ---------------- UPDATE USER----------------
    [HttpPut("UpdateUser/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateDto dto, [FromQuery] Guid adminId)
    {
        try
        {
            var refreshToken = Request.Cookies["sb-refresh-token"];
            if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
                return Unauthorized();

            Profile currentUser;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                currentUser = await _profilesService.GetCurrentUserProfileAsync(refreshToken);
            }
            else
            {
                currentUser = await _profilesService.GetCurrentUserProfileAsync(adminId);
            }

            if (currentUser == null || currentUser.Role != "admin")
                return Unauthorized("Usuario no autorizado");

            var result = await _profilesService.UpdateUserAsync(id, dto.Role, dto.Name);

            if (!result)
                return BadRequest("No hubo cambios o no autorizado");

            return Ok("Usuario actualizado");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
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
