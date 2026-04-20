using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/control")]
[ApiKey]
public class ControlController : ControllerBase
{
    private readonly IControlAppService _appService;

    public ControlController(IControlAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetStatusByName(string name)
    {
        try
        {
            var status = await _appService.GetStatusByName(name);
            return Ok(status);
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

    [HttpPut("{name}")]
    public async Task<IActionResult> UpdateStatusByName(string name)
    {
        try
        {
            var status = await _appService.UpdateStatusByName(name);
            return Ok(status);
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