using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/subjects")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectsAppService _appService;

    public SubjectsController(ISubjectsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _appService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var subject = await _appService.GetByIdAsync(id);
        return subject == null ? NotFound() : Ok(subject);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Subject subject)
        => Ok(await _appService.CreateAsync(subject));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Subject subject)
    {
        if (id != subject.Id) return BadRequest();
        await _appService.UpdateAsync(subject);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _appService.DeleteAsync(id);
        return NoContent();
    }
}
