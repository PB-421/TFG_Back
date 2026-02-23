using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly IGroupsAppService _appService;

    public GroupsController(IGroupsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _appService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var group = await _appService.GetByIdAsync(id);
        return group == null ? NotFound() : Ok(group);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Group group)
        => Ok(await _appService.CreateAsync(group));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Group group)
    {
        if (id != group.Id) return BadRequest();
        await _appService.UpdateAsync(group);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _appService.DeleteAsync(id);
        return NoContent();
    }
}
