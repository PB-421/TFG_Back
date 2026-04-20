using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Supabase;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string RefreshTokenCookieName = "sb-refresh-token";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var services = httpContext.RequestServices;
        
        // Obtenemos los servicios necesarios
        var profilesService = services.GetRequiredService<IProfilesAppService>();
        var supabase = services.GetRequiredService<Client>();

        // 1. Intento por API KEY (Secret Key)
        bool hasApiKey = httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey);
        var secureKey = Environment.GetEnvironmentVariable("CRON_SECRET_KEY");
        if (hasApiKey && !string.IsNullOrEmpty(secureKey) && secureKey.Equals(extractedApiKey))
        {
            await next();
            return;
        }

        // 2. Intento mediante el Cliente de Supabase
        var supabaseUser = supabase.Auth.CurrentUser;
        if (supabaseUser != null && !string.IsNullOrEmpty(supabaseUser.Id))
        {
            var profile = await profilesService.GetCurrentUserProfileAsync(Guid.Parse(supabaseUser.Id));
            if (profile != null)
            {
                httpContext.Items["UserProfile"] = profile;
                await next();
                return;
            }
        }

        // 3. Intento por COOKIE
        var refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];
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

        context.Result = new UnauthorizedObjectResult(new { 
            error = "Acceso denegado", 
            message = "No se encontró una sesión válida o API Key." 
        });
    }
}