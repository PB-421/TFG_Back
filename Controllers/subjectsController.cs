using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/subjects")]
[ApiKey]
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
        try
        {
            var listSubjects = await _subjectService.GetAllAsync();

            var dtoList = listSubjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Course = s.Course
            }).ToList();

            return Ok(dtoList);
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var subjects = await _subjectService.GetSubjectNamesByIds(new List<Guid> { id });
            if (subjects == null || subjects.Count == 0) return NotFound("Asignatura no encontrada");
            return Ok(subjects[0].Name);
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubjectDto subject)
    {
        try
        {
            var createdSubject = await _subjectService.CreateAsync(subject);
            if (!createdSubject) return BadRequest("La asignatura ya existe");

            return Ok("Asignatura creada");
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SubjectDto subject, [FromQuery] Guid adminId)
    {
        try
        {
            var updatedSubject = await _subjectService.UpdateAsync(id, subject);
            if (!updatedSubject) return BadRequest("La asignatura ya existe");

            return Ok("Asignatura actualizada");
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid adminId)
    {
        try
        {

            var deletedSubject = await _subjectService.DeleteAsync(id);
            if (!deletedSubject) return BadRequest("Error al borrar la asignatura");

            return Ok("Asignatura borrada");
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
