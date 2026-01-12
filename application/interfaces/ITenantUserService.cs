using System.Security.Claims;

namespace qa_portal_apis.application.interfaces;

public interface IUserProvisioningService
{
    Task EnsureUserExistsAsync(ClaimsPrincipal principal);
}
