using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;

namespace qa_portal_apis.infrastructure.persistence;

public class RoleRequestRepository : IRoleRequestRepository
{
    private readonly AppDbContext _db;

    public RoleRequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<RoleChangeRequest>> GetAllAsync() =>
        await _db.RoleChangeRequests.OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<IEnumerable<RoleChangeRequest>> GetByUserIdAsync(long userId) =>
        await _db.RoleChangeRequests.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<RoleChangeRequest?> GetByIdAsync(long id) =>
        await _db.RoleChangeRequests.FindAsync(id);

    public async Task AddAsync(RoleChangeRequest request)
    {
        _db.RoleChangeRequests.Add(request);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(RoleChangeRequest request)
    {
        _db.RoleChangeRequests.Update(request);
        await _db.SaveChangesAsync();
    }
}
