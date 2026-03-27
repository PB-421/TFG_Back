using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/algorithms")]
public class AlgorithmsController : ControllerBase
{
    private readonly IAlgorithmsAppService _appService;
    private readonly ISubjectsAppService _subjectsService;

    public AlgorithmsController(IAlgorithmsAppService appService, ISubjectsAppService subjectsService)
    {
        _appService = appService;
        _subjectsService = subjectsService;
    }

    [HttpGet("FillGroups")]
    public async Task<IActionResult> FillGroupsEvenly()
    {
        try
        {
            var subjects = await _subjectsService.GetAllAsync();
            
            if (subjects == null || !subjects.Any())
            {
                return NotFound("No se encontraron asignaturas para procesar.");
            }

            var results = new List<string>();

            foreach (var subject in subjects)
            {
                var (ok, error) = await _appService.DistributeStudentsRoundRobinAsync(subject.Id);
                
                if (ok)
                {
                    results.Add($"Asignatura {subject.Name}: Procesada con éxito.");
                }
                else
                {
                    results.Add($"Asignatura {subject.Name}: Error - {error}");
                }
            }

            return Ok(new { 
                Message = "Proceso de reparto finalizado", 
                Details = results 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
}