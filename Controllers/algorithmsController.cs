using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

[ApiController]
[Route("api/algorithms")]
[ApiKey]
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
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
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
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            return SupabaseErrorResponse(ex, 400);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error crítico en el proceso: {ex.Message}");
        }
    }

    private string ExtractSupabaseMessage(Supabase.Gotrue.Exceptions.GotrueException ex)
    {
        if (ex == null || string.IsNullOrEmpty(ex.Message))
            return "Error desconocido de Supabase";

        try
        {
            var json = JsonSerializer.Deserialize<JsonObject>(ex.Message);
            if (json != null)
            {
                var msg = json["msg"]?.ToString();
                if (!string.IsNullOrEmpty(msg)) return msg;

                var error = json["error"]?.ToString();
                if (!string.IsNullOrEmpty(error)) return error;

                var description = json["error_description"]?.ToString();
                if (!string.IsNullOrEmpty(description)) return description;

                var messageField = json["message"]?.ToString();
                if (!string.IsNullOrEmpty(messageField)) return messageField;

                return json.ToString();
            }

            return ex.Message;
        }
        catch
        {
            return ex.Message;
        }
    }

    private IActionResult SupabaseErrorResponse(Supabase.Gotrue.Exceptions.GotrueException ex, int statusCode = 400)
    {
        var mensaje = ExtractSupabaseMessage(ex);
        var payload = new { error = mensaje };
        return StatusCode(statusCode, payload);
    }
}