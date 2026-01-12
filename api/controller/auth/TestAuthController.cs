using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace qa_portal_apis.api.controller;

[ApiController]
public class TestAuthController : ControllerBase
{
    [HttpGet("/test/auth-status")]
    public IActionResult GetAuthStatus()
    {
        var result = new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType,
            Name = User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            HasCookie = Request.Cookies.ContainsKey("QAPortal.Auth"),
            AllCookies = Request.Cookies.Keys.ToList()
        };
        
        Console.WriteLine($"[TEST] Auth Status Check:");
        Console.WriteLine($"  IsAuthenticated: {result.IsAuthenticated}");
        Console.WriteLine($"  Has QAPortal.Auth cookie: {result.HasCookie}");
        Console.WriteLine($"  Total cookies: {result.AllCookies.Count}");
        
        return Ok(result);
    }
}
