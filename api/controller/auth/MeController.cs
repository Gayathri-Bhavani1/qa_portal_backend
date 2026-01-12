using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using qa_portal_apis.application.users;
using qa_portal_apis.infrastructure.auth;

namespace qa_portal_apis.api.controller.auth;

[ApiController]
public class MeController : ControllerBase
{
    private readonly GetOrCreateUser _getOrCreate;

    public MeController(GetOrCreateUser getOrCreate)
    {
        _getOrCreate = getOrCreate;
    }

    [AllowAnonymous]
    [HttpGet("api/me")]
    public async Task<IActionResult> Me()
    {
        Console.WriteLine($"[ME] IsAuthenticated: {User.Identity?.IsAuthenticated}, Name: {User.Identity?.Name}");
        
        if (User.Identity?.IsAuthenticated != true)
        {
            Console.WriteLine("[ME] User not authenticated, returning 200 (null) to parse as Guest");
            return Ok(null);
        }

        var email = User.Email();
        var sub = User.Subject();
        var provider = User.Provider();
        
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(provider))
        {
            Console.WriteLine("[ME] Missing required claims (email/sub/provider)");
            return BadRequest("Missing required user claims.");
        }

        var dto = await _getOrCreate.ExecuteAsync(provider, sub, email);

        Console.WriteLine($"[ME] Successfully retrieved user data for {email}");
        return Ok(dto);
    }
}