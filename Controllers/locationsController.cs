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
    {
        var locations = await _appService.GetAllAsync();
        return Ok(locations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Nota: He usado GetLocationById para coincidir con el nombre de tu AppService
        var location = await _appService.GetLocationById(id);
        return location == null ? NotFound() : Ok(location);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationDto locationDto)
    {
        if (locationDto == null) return BadRequest();

        var success = await _appService.CreateAsync(locationDto);
        
        // Es buena práctica devolver un 201 Created, pero mantenemos la lógica de éxito
        return success ? Ok() : BadRequest("Could not create the location.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LocationDto locationDto)
    {
        // Ahora pasamos el ID por separado y el DTO como cuerpo
        var success = await _appService.UpdateAsync(id, locationDto);
        
        if (!success) return NotFound("Location not found or no changes were made.");
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _appService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}