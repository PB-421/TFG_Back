using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        bool hasApiKey = httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey);
        var secureKey = Environment.GetEnvironmentVariable("CRON_SECRET_KEY");
        bool isApiKeyValid = hasApiKey && !string.IsNullOrEmpty(secureKey) && secureKey.Equals(extractedApiKey);

        bool isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;

        if (isApiKeyValid || isAuthenticated)
        {
            await next();
            return;
        }

        context.Result = new UnauthorizedObjectResult(new { 
            error = "Acceso denegado", 
            message = "Se requiere una API Key válida o una sesión de usuario activa." 
        });
    }
}