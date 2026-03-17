using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IProfilesAppService _profilesService;

    private readonly ISubjectsAppService _subjectsService;

    public ProfilesController(IProfilesAppService profilesService, ISubjectsAppService subjectsService)
    {
        _profilesService = profilesService;
        _subjectsService = subjectsService;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll([FromQuery] Guid adminId)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];
        if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
            return Unauthorized();

        try
        {
            var profiles = new List<Profile>();
            if(!string.IsNullOrEmpty(refreshToken)){
                profiles = await _profilesService.GetAllProfilesAsync(refreshToken!);
            } else
            {
                profiles = await _profilesService.GetAllProfilesAsync(adminId);
            }
            if (profiles == null || profiles.Count == 0) return Unauthorized();

            var tasks = profiles.Select(async p => new profileDto
            {
                Id = p.Id,
                Email = p.Email,
                Name = p.Name,
                Role = p.Role,
                Subjects = await _subjectsService.GetSubjectNamesByIds(p.Subjects)
            });

            var dtoList = await Task.WhenAll(tasks);

            return Ok(dtoList);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfileById(Guid id)
    {
        var profile = await _profilesService.GetProfileById(id);
        return Ok(profile);
    }

    // ---------------- UPDATE USER----------------
    [HttpPut("UpdateUser/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateDto dto, [FromQuery] Guid adminId)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];
        if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
            return Unauthorized();
        var currentUser = new Profile();
        if(!string.IsNullOrEmpty(refreshToken)){
            currentUser = await _profilesService.GetCurrentUserProfileAsync(refreshToken);
        } else
        {
            currentUser = await _profilesService.GetCurrentUserProfileAsync(adminId);
        }

        if (currentUser == null || currentUser.Role != "admin")
            return Unauthorized("Usuario no autorizado");

        var result = await _profilesService
            .UpdateUserAsync(id, dto.Role, dto.Name);

        if (!result)
            return BadRequest("No hubo cambios o no autorizado");

        return Ok("Usuario actualizado");
    }
}