using qa_portal_apis.domain.entities;

namespace qa_portal_apis.domain.interfaces;

public interface IUserRepository
{
    
    Task<User?> GetByIdentityAsync(string provider, string subjectId);
    Task<User> AddAsync(User user);
    Task AddUserRoleAsync(UserRole userRole);
    Task<int> GetUserRoleIdAsync(long userId);
    Task<bool> AnyUsersAsync();
    Task SaveChangesAsync();
}