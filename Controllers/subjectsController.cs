using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/subjects")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectsAppService _subjectService;

    private readonly IProfilesAppService _profileService;

    public SubjectsController(ISubjectsAppService subjectService, IProfilesAppService profileService)
    {
        _subjectService = subjectService;
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var listSubjects = await _subjectService.GetAllAsync();
    
        var dtoList = listSubjects.Select(s => new SubjectDto 
        { 
            Id = s.Id, 
            Name = s.Name 
        }).ToList();

        return Ok(dtoList);
    }


    [HttpPost]
    public async Task<IActionResult> Create(SubjectDto subject, [FromQuery] Guid adminId)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];
        if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
            return Unauthorized();
        var currentUser = new Profile();
        if(!string.IsNullOrEmpty(refreshToken)){
            currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);
        } else
        {
            currentUser = await _profileService.GetCurrentUserProfileAsync(adminId);
        }

        if (currentUser == null || currentUser.Role != "admin")
            return Unauthorized("Usuario no autorizado");
        
        var createdSubject = await _subjectService.CreateAsync(subject);
        if(!createdSubject) return BadRequest("Error al crear la asignatura");
        return Ok("Asignatura Creada"); 
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(SubjectDto subject, [FromQuery] Guid adminId)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];
        if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
            return Unauthorized();
        var currentUser = new Profile();
        if(!string.IsNullOrEmpty(refreshToken)){
            currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);
        } else
        {
            currentUser = await _profileService.GetCurrentUserProfileAsync(adminId);
        }

        if (currentUser == null || currentUser.Role != "admin")
            return Unauthorized("Usuario no autorizado");
        
        var updatedSubject = await _subjectService.UpdateAsync(subject);
        if(!updatedSubject)  return BadRequest("Error al actualizar la asignatura");
        return Ok("Asignatura actualizada");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid adminId)
    {
        var refreshToken = Request.Cookies["sb-refresh-token"];
        if (string.IsNullOrEmpty(refreshToken) && adminId == Guid.Empty)
            return Unauthorized();
        var currentUser = new Profile();
        if(!string.IsNullOrEmpty(refreshToken)){
            currentUser = await _profileService.GetCurrentUserProfileAsync(refreshToken);
        } else
        {
            currentUser = await _profileService.GetCurrentUserProfileAsync(adminId);
        }

        if (currentUser == null || currentUser.Role != "admin")
            return Unauthorized("Usuario no autorizado");
        
        var deletedSubject = await _subjectService.DeleteAsync(id);
        if(!deletedSubject)  return BadRequest("Error al borrar la asignatura");
        return Ok("Asignatura borrada");
    }
}
