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
    {
        var groups = await _appService.GetAllAsync();
        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GroupsDto dto)
    {
        var success = await _appService.CreateAsync(dto);
        if (!success) return BadRequest("No se pudo crear el grupo.");
        
        return Ok("Grupo Creado");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] GroupsDto dto)
    {
        var success = await _appService.UpdateAsync(id, dto);
        if (!success) return BadRequest("Grupo no actualizado");
        
        return Ok("Grupo actualizado");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _appService.DeleteAsync(id);
        if (!success) return NotFound();
        
        return NoContent();
    }
}
