using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/subjects")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectsAppService _appService;

    public SubjectsController(ISubjectsAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var listSubjects = await _appService.GetAllAsync();
    
        var dtoList = listSubjects.Select(s => new SubjectDto 
        { 
            Id = s.Id, 
            Name = s.Name 
        }).ToList();

        return Ok(dtoList);
    }


    [HttpPost]
    public async Task<IActionResult> Create(SubjectDto subject)
    {
        var createdSubject = await _appService.CreateAsync(subject);
        if(!createdSubject) return BadRequest("Error al crear la asignatura");
        return Ok("Asignatura Creada"); 
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(SubjectDto subject)
    {
        var updatedSubject = await _appService.UpdateAsync(subject);
        if(!updatedSubject)  return BadRequest("Error al actualizar la asignatura");
        return Ok("Asignatura actualizada");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deletedSubject = await _appService.DeleteAsync(id);
        if(!deletedSubject)  return BadRequest("Error al borrar la asignatura");
        return Ok("Asignatura borrada");
    }
}
