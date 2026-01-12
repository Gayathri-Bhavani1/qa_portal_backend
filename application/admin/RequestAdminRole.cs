using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.enums;
using qa_portal_apis.infrastructure.persistence;

namespace qa_portal_apis.application.admin;

public class RequestAdminRole
{
    private readonly AppDbContext _db;
    public RequestAdminRole(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExecuteAsync(long userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if(user is null) return  false;
        if(user.Role == Role.Admin)
            return  true;
        user.ApprovalStatus = ApprovalStatus.Pending;
        
        await _db.SaveChangesAsync();
        return true;
    }
}