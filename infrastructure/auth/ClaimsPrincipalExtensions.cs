using System.Security.Claims;

namespace qa_portal_apis.infrastructure.auth;

public static class ClaimsPrincipalExtensions
{
    public static string? Email(this ClaimsPrincipal user) =>
        user.FindFirst("preferred_username")?.Value ??
        user.FindFirst("upn")?.Value ??
        user.FindFirst("email")?.Value ??
        user.FindFirst(ClaimTypes.Email)?.Value;

    public static string? Subject(this ClaimsPrincipal user) =>
        user.FindFirst("oid")?.Value ??
        user.FindFirst("sub")?.Value ??
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string Provider(this ClaimsPrincipal user) =>
        user.Identity?.AuthenticationType ?? "idp";
}