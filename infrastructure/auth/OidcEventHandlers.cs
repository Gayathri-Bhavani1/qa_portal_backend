using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using qa_portal_apis.application.interfaces;

namespace qa_portal_apis.infrastructure.auth;

public static class OidcEventHandlers
{
    public static async Task HandleTicketReceived(TicketReceivedContext context)
    {
        Console.WriteLine("OIDC Event: OnTicketReceived");

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        
        var userService = context.HttpContext.RequestServices
            .GetRequiredService<IUserProvisioningService>();

        // Ensure Tenant + User exist
        if (context.Principal != null) 
        {
            try 
            {
                Console.WriteLine($"Provisioning user: {context.Principal.Identity?.Name}");
                
                await userService.EnsureUserExistsAsync(context.Principal);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error provisioning user: {ex.Message}");
                logger.LogError(ex, "Failed to ensure user in OnTicketReceived");
            }
        }

        logger.LogInformation("OnTicketReceived: Authentication successful. Database sync attempted.");
    }

    public static Task HandleApiUnauthenticated(RedirectContext context)
    {
        // Prevent 302 Redirects for API calls (CORS Fix)
        // This stops the browser from hijacking background fetches and redirecting the whole page to SSO
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api") && 
            !path.StartsWithSegments("/api/login") && 
            !path.StartsWithSegments("/api/logout"))
        {
           Console.WriteLine($"OIDC Event: OnRedirectToIdentityProvider - Intercepted for API: {path}");
           Console.WriteLine("  API Request detected. Returning 401 instead of Redirect to prevent JSON takeover.");
           
           var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

           // Manually add CORS headers to ensure the browser accepts the response
           if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
           {
               var corsOrigin = configuration["FrontendUrl"] ?? "https://localhost:5173";
               if (corsOrigin.EndsWith("/")) corsOrigin = corsOrigin.TrimEnd('/');
               
               context.Response.Headers.Append("Access-Control-Allow-Origin", corsOrigin);
               context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
               context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
           }
           
           context.Response.StatusCode = 401; // Return 401 so frontend authService can handle it
           context.HandleResponse();
        }
        
        return Task.CompletedTask;
    }

    public static async Task ConfigureLogoutRequest(RedirectContext context)
    {
         Console.WriteLine("OIDC Event: OnRedirectToIdentityProviderForSignOut");

         // 1. Set the callback URL
         
         var backendCallback = "https://localhost:5075/signout-callback-oidc";
         
         Console.WriteLine($"  Setting post_logout_redirect_uri to: {backendCallback}");
         context.ProtocolMessage.PostLogoutRedirectUri = backendCallback;

         // 2. Add id_token_hint to skip "Choose account" prompt
         var idToken = await context.HttpContext.GetTokenAsync("id_token");
         if (!string.IsNullOrEmpty(idToken))
         {
             Console.WriteLine("  Adding id_token_hint to logout request");
             context.ProtocolMessage.IdTokenHint = idToken;
         }
    }

    public static Task HandleSignedOutCallback(RemoteSignOutContext context)
    {
        Console.WriteLine("OIDC Event: OnSignedOutCallbackRedirect");
        
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        
        // Redirect to frontend auth-callback
        var frontendUrl = configuration["FrontendUrl"] ?? "https://localhost:5173";
        var finalUrl = $"{frontendUrl}/auth-callback";
        
        Console.WriteLine($"  Final Redirect to: {finalUrl}");
        context.Response.Redirect(finalUrl);
        context.HandleResponse();
        
        return Task.CompletedTask;
    }

    public static Task HandleAuthenticationFailed(AuthenticationFailedContext context)
    {
        Console.WriteLine($"OIDC Event: OnAuthenticationFailed. Error: {context.Exception.Message}");

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        
        logger.LogError(context.Exception, 
            "Authentication failed: {Message}", 
            context.Exception.Message);
        
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173/";
        context.Response.Redirect($"{frontendUrl}/login?error=auth_failed");
        context.HandleResponse();
        
        return Task.CompletedTask;
    }

    public static Task HandleRemoteFailure(RemoteFailureContext context)
    {
        Console.WriteLine($"OIDC Event: OnRemoteFailure. Error: {context.Failure?.Message}");

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        
        logger.LogError(context.Failure, 
            "Remote authentication failed: {Message}", 
            context.Failure?.Message);
        
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";
        var errorMessage = Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed");
        
        context.Response.StatusCode = 302;
        context.Response.Headers.Location = $"{frontendUrl}/login?error={errorMessage}";
        context.HandleResponse();
        
        return Task.CompletedTask;
    }
}
