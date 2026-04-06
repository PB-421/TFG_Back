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

    [HttpGet("CheckRequests")]
    public async Task<IActionResult> CheckRequests()
    {
        try
        {
            // PASO 1: Ejecutar el algoritmo MCMF para marcar solicitudes como aceptadas (Status 2)
            var (okMcmf, messageMcmf) = await _appService.ResolveWithMinCostFlowAsync();
            
            if (!okMcmf)
            {
                return BadRequest(new { Error = "Error al ejecutar el algoritmo", Details = messageMcmf });
            }

            // PASO 2: Aplicar los cambios de las solicitudes aceptadas a los grupos
            var (okApply, messageApply) = await _appService.ApplyAcceptedRequestsAsync();

            if (!okApply)
            {
                return StatusCode(500, new { Error = "Error al aplicar los cambios", Details = messageApply });
            }

            return Ok(new 
            { 
                Message = "Optimización completada con éxito", 
                AlgorithmResult = messageMcmf,
                ApplyResult = messageApply 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error crítico en el proceso: {ex.Message}");
        }
    }
}