using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace qa_portal_apis.api.controller.auth;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("api/login")]
    public IActionResult Login()
    {
        // Always redirect to auth-callback after login to support popup flow
        var frontendUrl = _config["FrontendUrl"] ?? "https://localhost:5173";
        var redirectUri = $"{frontendUrl}/auth-callback";

        Console.WriteLine("\n[DEBUG] === Login Request ===");
        if (User.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine("[DEBUG] User is already authenticated. Redirecting to: " + redirectUri);
            return Redirect(redirectUri);
        }

        Console.WriteLine("[DEBUG] Triggering OpenIdConnect Challenge with RedirectUri: " + redirectUri);
        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        return Challenge(properties, Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("api/logout")]
    public IActionResult Logout()
    {
        // Always redirect to auth-callback after logout to support popup flow
        var frontendUrl = _config["FrontendUrl"] ?? "https://localhost:5173";
        var redirectUri = $"{frontendUrl}/auth-callback";
        
        Console.WriteLine("\n[DEBUG] Logout requested");
        Console.WriteLine($"[DEBUG] Will redirect to: {redirectUri}");
        
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };
        
        return SignOut(properties, "Cookies", "OpenIdConnect");
    }
}