using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/schedules")]
public class SchedulesController : ControllerBase
{
    private readonly ISchedulesAppService _appService;

    public SchedulesController(ISchedulesAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _appService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var schedule = await _appService.GetByIdAsync(id);
        return schedule == null ? NotFound() : Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Schedule schedule)
        => Ok(await _appService.CreateAsync(schedule));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Schedule schedule)
    {
        if (id != schedule.Id) return BadRequest();
        await _appService.UpdateAsync(schedule);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _appService.DeleteAsync(id);
        return NoContent();
    }
}
