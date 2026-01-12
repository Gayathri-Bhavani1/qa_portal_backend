namespace qa_portal_apis.application.users.dtos;

public record UserDto
(
    string UserId,
    string TenantId,
    string Email,
    string DisplayName,
    bool IsActive,
    string CreatedAt,
    string Role,
    string ApprovalStatus
);