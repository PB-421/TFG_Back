using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

[ApiController]
[Route("api/subjects")]
[ApiKey]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectsAppService _subjectService;

    public SubjectsController(ISubjectsAppService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var listSubjects = await _subjectService.GetAllAsync();

            var dtoList = listSubjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Course = s.Course
            }).ToList();

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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var subjects = await _subjectService.GetSubjectNamesByIds(new List<Guid> { id });
            if (subjects == null || subjects.Count == 0) return NotFound("Asignatura no encontrada");
            return Ok(subjects[0].Name);
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
    public async Task<IActionResult> Create([FromBody] SubjectDto subject)
    {
        try
        {
            var createdSubject = await _subjectService.CreateAsync(subject);
            if (!createdSubject) return BadRequest("La asignatura ya existe");

            return Ok("Asignatura creada");
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
    public async Task<IActionResult> Update(Guid id, [FromBody] SubjectDto subject, [FromQuery] Guid adminId)
    {
        try
        {
            var updatedSubject = await _subjectService.UpdateAsync(id, subject);
            if (!updatedSubject) return BadRequest("La asignatura ya existe");

            return Ok("Asignatura actualizada");
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
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid adminId)
    {
        try
        {

            var deletedSubject = await _subjectService.DeleteAsync(id);
            if (!deletedSubject) return BadRequest("Error al borrar la asignatura");

            return Ok("Asignatura borrada");
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
