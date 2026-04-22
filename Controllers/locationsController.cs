using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

[ApiController]
[Route("api/locations")]
[ApiKey]
public class LocationsController : ControllerBase
{
    private readonly ILocationsAppService _appService;

    public LocationsController(ILocationsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var locations = await _appService.GetAllAsync();
            return Ok(locations);
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
            var location = await _appService.GetLocationById(id);
            if (location == null) return NotFound("Ubicación no encontrada");
            return Ok(location);
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
    public async Task<IActionResult> Create([FromBody] LocationDto locationDto)
    {
        try
        {
            if (locationDto == null) return BadRequest("Payload inválido");

            var success = await _appService.CreateAsync(locationDto);
            if (!success) return BadRequest("El nombre ya existe en otra localizacion");

            return Ok("Localizacion creada con exito");
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
    public async Task<IActionResult> Update(Guid id, [FromBody] LocationDto locationDto)
    {
        try
        {
            if (locationDto == null) return BadRequest("Payload inválido");

            var success = await _appService.UpdateAsync(id, locationDto);
            if (!success) return NotFound("El nombre ya existe en otra localizacion");

            return Ok("Localizacion actualizada");
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
            return success ? NoContent() : NotFound("Ubicación no encontrada");
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
