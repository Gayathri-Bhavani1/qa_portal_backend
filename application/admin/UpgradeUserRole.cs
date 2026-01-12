using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.enums;
using qa_portal_apis.infrastructure.persistence;

namespace qa_portal_apis.application.admin;

public class UpgradeUserRole
{
    private readonly AppDbContext _db;
    public UpgradeUserRole(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExecuteAsync(long targetUserId, long actingUserId, bool actingUserIsAdmin)
    {
        var target = await _db.Users.FirstOrDefaultAsync(x => x.Id == targetUserId);
        if(target is null) return  false;

        var adminExists = await _db.Users.AnyAsync((x => x.Role == Role.Admin));
        if (!adminExists)
        {
            target.Role = Role.Admin;
            target.ApprovalStatus = ApprovalStatus.Approved;
            await _db.SaveChangesAsync();
            return true;
        }

        if (!actingUserIsAdmin)
        {
            return false;
        }

        target.Role = Role.Admin;
        target.ApprovalStatus = ApprovalStatus.Approved;
        await _db.SaveChangesAsync();
        return true;

    }
}