using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

[ApiController]
[Route("api/requests")]
[ApiKey]
public class RequestsController : ControllerBase
{
    private readonly IRequestsAppService _appService;

    public RequestsController(IRequestsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var requests = await _appService.GetAllAsync();
            return Ok(requests);
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

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(Guid studentId)
    {
        try
        {
            var requests = await _appService.GetByStudentId(studentId);
            return Ok(requests);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al obtener peticiones: {ex.Message}");
        }
    }

    [HttpGet("teacher/{teacherId}")]
    public async Task<IActionResult> GetByTeacherId(Guid teacherId)
    {
        try
        {
            var requests = await _appService.GetByTeacherId(teacherId);
            return Ok(requests);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al obtener peticiones: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var request = await _appService.GetByIdAsync(id);
            if (request == null || request.Id == Guid.Empty) 
                return NotFound("Solicitud no encontrada");
                
            return Ok(request);
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RequestDto requestDto)
    {
        try
        {
            if (requestDto == null) return BadRequest("Payload inválido");

            var success = await _appService.CreateAsync(requestDto);
            if (!success) return BadRequest("No puedes tener 2 solicitudes de la misma asignatura a la vez");

            return Ok("Solicitud creada con éxito");
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RequestDto requestDto)
    {
        try
        {
            if (requestDto == null) return BadRequest("Payload inválido");

            var success = await _appService.UpdateAsync(id, requestDto);
            if (!success) return NotFound("No se encontró la solicitud o no hubo cambios para actualizar");

            return Ok("Solicitud actualizada");
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

    [HttpPut("teacherUpdate/{id}")]
    public async Task<IActionResult> UpdateFromTeacher(Guid id, [FromBody] RequestUpdateDto requestDto)
    {
        try
        {
            if (requestDto == null) return BadRequest("Payload inválido");

            var success = await _appService.UpdateFromTeacherAsync(id, requestDto);
            if (!success) return NotFound("No se encontró la solicitud o no hubo cambios para actualizar");

            return Ok("Solicitud actualizada");
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _appService.DeleteAsync(id);
            return success ? Ok("Solicitud borrada") : NotFound("Solicitud no encontrada");
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

    [HttpDelete("completed")]
    public async Task<IActionResult> DeleteCompleted()
    {
        try
        {
            await _appService.DeleteCompletedRequest();
            return Ok("Las solicitudes completadas se han borrado correctamente");
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