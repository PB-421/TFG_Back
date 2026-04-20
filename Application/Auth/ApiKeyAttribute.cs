using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Supabase;
using System.Security.Claims;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string RefreshTokenCookieName = "sb-refresh-token";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
    var httpContext = context.HttpContext;
    var services = httpContext.RequestServices;
    var profilesService = services.GetRequiredService<IProfilesAppService>();
    var supabase = services.GetRequiredService<Client>();

    // 1. EXTRAER TOKEN Y COOKIE
    string? authHeader = httpContext.Request.Headers["Authorization"];
    string? refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];

    // 2. SI HAY BEARER TOKEN, INICIALIZAR SESIÓN EN BACKEND
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
    {   
        var token = authHeader.Substring("Bearer ".Length).Trim();
        try 
        {
            await supabase.Auth.SetSession(token, refreshToken ?? ""); 
        }
        catch { /* Token inválido o expirado */ }
    }

    // --- FLUJO DE VALIDACIÓN (En orden de prioridad) ---

    // A. Intento por API KEY (Para procesos automáticos/Cron)
    if (httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
    {
        var secureKey = Environment.GetEnvironmentVariable("CRON_SECRET_KEY");
        if (!string.IsNullOrEmpty(secureKey) && secureKey.Equals(extractedApiKey.ToString()))
        {
            await next();
            return;
        }
    }

    // B. Intento por REFRESH TOKEN (Cookie) - Como respaldo
    if (!string.IsNullOrEmpty(refreshToken))
    {
        var profile = await profilesService.GetCurrentUserProfileAsync(refreshToken);
        if (profile != null)
        {
            httpContext.Items["UserProfile"] = profile; 
            await next();
            return;
        }
    }

    // C. Intento por USUARIO DE SUPABASE (Bearer Token o Sesión activa)
    var supabaseUser = supabase.Auth.CurrentUser;
    if (supabaseUser != null && !string.IsNullOrEmpty(supabaseUser.Id))
    {
        var profile = await profilesService.GetCurrentUserProfileAsync(Guid.Parse(supabaseUser.Id));
        if (profile != null)
        {
            httpContext.Items["UserProfile"] = profile; // Guardamos para el controller
            await next();
            return;
        }
    }

    // Si llegamos aquí, nadie es válido
    context.Result = new UnauthorizedObjectResult(new { 
        error = "Acceso denegado", 
        message = "No se detectó una sesión válida. Asegúrate de enviar el token en el header Authorization." 
    });
}
}