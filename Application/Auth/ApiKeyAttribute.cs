using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

    string? authHeader = httpContext.Request.Headers["Authorization"];
    string? refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
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

    // B. Intento por REFRESH TOKEN (Cookie)
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

    // C. Intento por USUARIO DE SUPABASE
    if (!string.IsNullOrEmpty(authHeader))
    {
        var profile = await profilesService.GetCurrentUserProfileAsync(Guid.Parse(authHeader));
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