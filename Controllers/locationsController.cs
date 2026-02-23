using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationsAppService _appService;

    public LocationsController(ILocationsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _appService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var location = await _appService.GetByIdAsync(id);
        return location == null ? NotFound() : Ok(location);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Location location)
        => Ok(await _appService.CreateAsync(location));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Location location)
    {
        if (id != location.Id) return BadRequest();
        await _appService.UpdateAsync(location);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _appService.DeleteAsync(id);
        return NoContent();
    }
}
