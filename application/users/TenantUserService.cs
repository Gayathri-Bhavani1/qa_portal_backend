using System.Security.Claims;
using qa_portal_apis.application.interfaces;
using qa_portal_apis.infrastructure.auth;

namespace qa_portal_apis.application.users;

public class UserProvisioningService : IUserProvisioningService
{
    private readonly GetOrCreateUser _getOrCreateUser;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(GetOrCreateUser getOrCreateUser, ILogger<UserProvisioningService> logger)
    {
        _getOrCreateUser = getOrCreateUser;
        _logger = logger;
    }

    public async Task EnsureUserExistsAsync(ClaimsPrincipal principal)
    {
        var email = principal.Email();
        var subjectId = principal.Subject();
        var provider = principal.Provider();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(subjectId))
        {
            _logger.LogWarning("Insufficient claims to ensure user: Email={Email}, SubjectId={SubjectId}", email, subjectId);
            return;
        }

        try
        {
            // Just create/update the user - no tenant needed
            await _getOrCreateUser.ExecuteAsync(provider, subjectId, email);
            _logger.LogInformation("Successfully ensured user {Email} in database", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring user {Email} in database", email);
            throw;
        }
    }
}
