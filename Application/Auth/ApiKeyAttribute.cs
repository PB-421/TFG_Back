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

        // 1. Obtener tokens (Corrigiendo error de nulos con string?)
        string? authHeader = httpContext.Request.Headers["Authorization"];
        string? refreshToken = httpContext.Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {   
            var token = authHeader.Substring("Bearer ".Length).Trim();
            try 
            {
                await supabase.Auth.SetSession(token, refreshToken ?? ""); 
            }
            catch { /* Token inválido */ }
        }

        // 2. Intento por API KEY
        if (httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            var secureKey = Environment.GetEnvironmentVariable("CRON_SECRET_KEY");
            if (!string.IsNullOrEmpty(secureKey) && secureKey.Equals(extractedApiKey.ToString()))
            {
                await next();
                return;
            }
        }

        // 3. Intento mediante el Cliente de Supabase (Ya sea por Bearer o Session)
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

        // 4. Intento por COOKIE de forma manual (Respaldo)
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
            message = "Se requiere una sesión válida de Azure/Supabase o una API Key." 
        });
    }
}