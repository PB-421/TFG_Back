using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IProfilesAppService _profilesService;

    public ProfilesController(IProfilesAppService profilesService)
    {
        _profilesService = profilesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        try
        {
            var profiles = await _profilesService
                .GetAllProfilesAsync(refreshToken);

            return Ok(profiles);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}