using qa_portal_apis.application.users.dtos;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.enums;
using qa_portal_apis.domain.interfaces;

namespace qa_portal_apis.application.users;

public class GetOrCreateUser
{
    private readonly IUserRepository _repo;
    
    public GetOrCreateUser(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<UserDto> ExecuteAsync(string provider, string subjectId, string email)
    {
        var user = await _repo.GetByIdentityAsync(provider, subjectId);

        if (user is null)
        {
            var isFirstUser = !await _repo.AnyUsersAsync();
            
            user = new User
            {
                Email = email,
                Name = email.Split('@')[0],
                IdpProvider = provider,
                IdpSubjectId = subjectId,
                Status = UserStatus.Active,
                ApprovalStatus = isFirstUser ? ApprovalStatus.Approved : ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(user);
            
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = isFirstUser ? 4 : 1, // 4 = super_admin, 1 = default_user
                AssignedBy = null, // System-assigned
                AssignedAt = DateTime.UtcNow
            };
            await _repo.AddUserRoleAsync(userRole);
        }

        // Ensure role exists even if user already existed (Self-healing)
        var existingRoleId = await _repo.GetUserRoleIdAsync(user.Id);
        if (existingRoleId == 0)
        {
            var defaultRole = new UserRole
            {
                UserId = user.Id,
                RoleId = 1, // Default
                AssignedBy = null, // System-assigned
                AssignedAt = DateTime.UtcNow
            };
            await _repo.AddUserRoleAsync(defaultRole);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();
        
        return new UserDto(
            UserId: user.Id.ToString(),
            TenantId: "default",
            Email: user.Email,
            DisplayName: user.Name,
            IsActive: user.Status == UserStatus.Active,
            CreatedAt: user.CreatedAt.ToString("O"),
            Role: await GetRoleNameAsync(user.Id),
            ApprovalStatus: user.ApprovalStatus.ToString()
        );
    }

    private async Task<string> GetRoleNameAsync(long userId)
    {
        var roleId = await _repo.GetUserRoleIdAsync(userId);
        return roleId switch
        {
            1 => "default_user",
            2 => "User",
            3 => "Admin", 
            4 => "super_admin",
            _ => "User" // Fallback
        };
    }
}