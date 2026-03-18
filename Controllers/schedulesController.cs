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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] List<SchedulesDto> dtos)
    {
        try
        {
            if (dtos == null || dtos.Count == 0) return BadRequest("Payload inválido o lista vacía");

            foreach (var dto in dtos)
            {
                var success = await _appService.CreateAsync(dto);
                if (!success)
                    return BadRequest("No se pudo crear el horario. Verifique si la ubicación está ocupada o si los datos son correctos.");
            }

            return Ok("Horarios creados");
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
    public async Task<IActionResult> Update(Guid id, [FromBody] SchedulesDto dto)
    {
        try
        {
            if (dto == null) return BadRequest("Payload inválido");

            var success = await _appService.UpdateAsync(id, dto);
            if (!success)
                return BadRequest("No se pudo actualizar. Es posible que el ID no exista o haya un conflicto de horario.");

            return Ok("Horario actualizado");
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
            if (!success) return NotFound("Horario no encontrado");
            return Ok("Horario borrado");
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
}
