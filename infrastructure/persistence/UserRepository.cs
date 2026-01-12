

using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;

namespace qa_portal_apis.infrastructure.persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdentityAsync(string provider, string subjectId) =>
        _db.Users.FirstOrDefaultAsync(u =>
            u.IdpProvider == provider &&
            u.IdpSubjectId == subjectId);

    public async Task<User> AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task AddUserRoleAsync(UserRole userRole)
    {
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUserRoleIdAsync(long userId)
    {
        var role = await _db.UserRoles
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.AssignedAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync();
        return role?.RoleId ?? 0;
    }

    public Task<bool> AnyUsersAsync() =>
        _db.Users.AnyAsync();

    public Task SaveChangesAsync() =>
        _db.SaveChangesAsync();
}