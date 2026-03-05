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

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        try
        {
            var profiles = await _profilesService
                .GetAllProfilesAsync(refreshToken);
            if(profiles.Count == 0) return Unauthorized();
            var dtoList = profiles.Select(p => new profileDto
            {
                Id = p.Id,
                Email = p.Email,
                Name = p.Name,
                Role = p.Role,
                Subjects = p.Subjects.ToArray()
            }).ToList();
            return Ok(dtoList);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}