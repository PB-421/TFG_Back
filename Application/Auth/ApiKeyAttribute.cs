using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string RefreshTokenCookieName = "sb-refresh-token";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var profilesService = httpContext.RequestServices.GetRequiredService<IProfilesAppService>();

        // 1. Intento por API KEY (Secret Key)
        bool hasApiKey = httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey);
        var secureKey = Environment.GetEnvironmentVariable("CRON_SECRET_KEY");
        bool isApiKeyValid = hasApiKey && !string.IsNullOrEmpty(secureKey) && secureKey.Equals(extractedApiKey);

        if (isApiKeyValid)
        {
            await next();
            return;
        }

        // 2. Intento por COOKIE
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var profile = await profilesService.GetCurrentUserProfileAsync(refreshToken);
            if (profile != null)
            {
                // Opcional: Guardar el perfil en el contexto para usarlo en el controller
                httpContext.Items["UserProfile"] = profile; 
                await next();
                return;
            }
        }

        // 3. Intento por OAUTH
        // Extraemos el ID del NameIdentifier
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? httpContext.User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out Guid userId) && userId != Guid.Empty)
        {
            var profile = await profilesService.GetCurrentUserProfileAsync(userId);
            if (profile != null)
            {
                httpContext.Items["UserProfile"] = profile;
                await next();
                return;
            }
        }

        // Si ninguna de las 3 condiciones se cumple:
        context.Result = new UnauthorizedObjectResult(new { 
            error = "Acceso denegado", 
            message = "No se detectó API Key válida, cookie de sesión o autenticación OAuth activa." 
        });
    }
}