using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly IRequestsAppService _appService;

    public RequestsController(IRequestsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _appService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = await _appService.GetByIdAsync(id);
        return request == null ? NotFound() : Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Request request)
        => Ok(await _appService.CreateAsync(request));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Request request)
    {
        if (id != request.Id) return BadRequest();
        await _appService.UpdateAsync(request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _appService.DeleteAsync(id);
        return NoContent();
    }
}
