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
        var schedules = await _appService.GetAllAsync();
        return Ok(schedules);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var schedules = await _appService.GetAllAsync();
        var schedule = schedules.FirstOrDefault(s => s.Id == id);
        return schedule == null ? NotFound() : Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> Create(List<SchedulesDto> dtos)
    {
        foreach(var dto in dtos){
            var success = await _appService.CreateAsync(dto);
            if (!success) 
                return BadRequest("No se pudo crear el horario. Verifique si la ubicación está ocupada o si los datos son correctos.");
        }
        
        return Ok("Horarios creados");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, SchedulesDto dto)
    {
        var success = await _appService.UpdateAsync(id, dto);
        if (!success) 
            return BadRequest("No se pudo actualizar. Es posible que el ID no exista o haya un conflicto de horario.");
        
        return Ok("Horario actualizado");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _appService.DeleteAsync(id);
        if (!success) return NotFound();
        
        return Ok("Horario borrado");
    }

    [HttpGet("capacity/{groupId}")]
    public async Task<IActionResult> GetCapacity(Guid groupId)
    {
        var groupLocations = await _appService.GetLocationsById(groupId);
        var capacity = await _locationService.GetLocationsCapacityByIds(groupLocations);
        return Ok(new { GroupId = groupId, Capacity = capacity });
    }
}