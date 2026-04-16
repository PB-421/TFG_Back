using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/schedules")]
public class SchedulesController : ControllerBase
{
    private readonly ISchedulesAppService _appService;
    private readonly ILocationsAppService _locationService;

    public SchedulesController(ISchedulesAppService appService, ILocationsAppService locaionService)
    {
        _appService = appService;
        _locationService = locaionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var schedules = await _appService.GetAllAsync();
            return Ok(schedules);
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
            var schedules = await _appService.GetAllAsync();
            var schedule = schedules.FirstOrDefault(s => s.Id == id);
            if (schedule == null) return NotFound("Horario no encontrado");
            return Ok(schedule);
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

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetSchedulesByGroupId(Guid groupId)
    {
        try
        {
            var schedules = await _appService.GetSchedulesByGroupIdAsync(groupId);
            return Ok(schedules);
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

    [HttpGet("groupCapacity/{groupId}")]
    public async Task<IActionResult> GetGroupCapacityByGroupId(Guid groupId)
    {
        try
        {
            var capacity = await _appService.GetGroupCapacityByGroupId(groupId);
            return Ok(capacity);
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

    [HttpGet("hasLocation/{locationId}")]
    public async Task<IActionResult> hasLocation(Guid locationId)
    {
        try
        {
            var exists = await _appService.LocationInUse(locationId);
            return Ok(exists);
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
    public async Task<IActionResult> Create([FromBody] List<SchedulesDto> dtos)
    {
        try
        {
            if (dtos == null || dtos.Count == 0) return BadRequest("Payload inválido");

            foreach (var dto in dtos)
            {
                await _appService.CreateAsync(dto);
            }

            return Ok("Sesion creada correctamente");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(FormatConflictMessage(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SchedulesDto dto)
    {
        try
        {
            if (dto == null) return BadRequest("Payload inválido");

            await _appService.UpdateAsync(id, dto);

            return Ok("Sesion actualizada");
        }
        catch (InvalidOperationException ex)
        {
           return BadRequest(FormatConflictMessage(ex.Message));
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
            if (!success) return NotFound("Sesion no encontrada");
            return Ok("Sesion borrada correctamente");
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

    [HttpGet("capacity/{groupId}")]
    public async Task<IActionResult> GetCapacity(Guid groupId)
    {
        try
        {
            var groupLocations = await _appService.GetLocationsById(groupId);
            if (groupLocations == null || !groupLocations.Any())
                return NotFound("No se encontraron ubicaciones para el grupo especificado");

            var capacity = await _locationService.GetLocationsCapacityByIds(groupLocations);
            return Ok(new { GroupId = groupId, Capacity = capacity });
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

    private string FormatConflictMessage(string exceptionMessage)
    {
        var parts = exceptionMessage.Split('|');
        if (parts.Length < 2) return exceptionMessage;

        var errorCode = parts[0];
        
        string FormatDateTime(string dt) => $"el día {dt.Split(' ')[0]} a las {dt.Split(' ')[1]}";

        switch (errorCode)
        {
            case "LOCATION_OCCUPIED":
                return $"El aula ya está ocupada {FormatDateTime(parts[1])}.";

            case "GROUP_OCCUPIED":
                return $"El grupo seleccionado ya tiene otra sesión programada {FormatDateTime(parts[1])}.";

            case "TEACHER_OCCUPIED":
                return $"El profesor ya tiene otra clase programada en otro grupo {FormatDateTime(parts[1])}.";

            case "SUBJECT_CONFLICT":
                return $"Conflicto de horario: otra asignatura del mismo curso tiene sesión {FormatDateTime(parts[1])}.";

            default:
                return exceptionMessage;
        }
    }
}
