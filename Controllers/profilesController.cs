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
    public async Task<IActionResult> GetAll()
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        try
        {
            var profiles = await _profilesService.GetAllProfilesAsync(refreshToken);
            if (profiles == null || profiles.Count == 0) return Unauthorized();

            var tasks = profiles.Select(async p => new profileDto
            {
                Id = p.Id,
                Email = p.Email,
                Name = p.Name,
                Role = p.Role,
                Subjects = await _subjectsService.GetNamesByIds(p.Subjects)
            });

            var dtoList = await Task.WhenAll(tasks);

            return Ok(dtoList);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // ---------------- UPDATE USER----------------
    [HttpPut("UpdateUser/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateDto dto)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Usuario no autorizado");

        var result = await _profilesService
            .UpdateUserAsync(id, dto.Role, dto.Name, refreshToken);

        if (!result)
            return BadRequest("No hubo cambios o no autorizado");

        return Ok("Usuario actualizado");
    }
}