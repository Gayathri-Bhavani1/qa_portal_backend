using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.enums;
using qa_portal_apis.domain.interfaces;
using qa_portal_apis.infrastructure.auth;
using qa_portal_apis.infrastructure.persistence;

namespace qa_portal_apis.api.controller.users;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db; // Using DB context directly for simplicity in user management
    private readonly IUserRepository _userRepo;

    public UsersController(AppDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var caller = await GetCurrentUserAsync();
        if (caller == null) return Unauthorized();

        var callerRole = await _userRepo.GetUserRoleIdAsync(caller.Id);
        if (callerRole < 3) return Forbid(); // Admin+ only

        var users = await _db.Users.ToListAsync();
        var dtos = new List<object>();

        foreach (var u in users)
        {
            dtos.Add(new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                role = await GetRoleNameAsync(u.Id),
                approvalStatus = u.ApprovalStatus.ToString(),
                createdAt = u.CreatedAt,
                lastLoginAt = u.LastLoginAt,
                isActive = u.Status == UserStatus.Active
            });
        }

        return Ok(dtos);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateRoleDto dto)
    {
        var caller = await GetCurrentUserAsync();
        if (caller == null) return Unauthorized();

        var callerRole = await _userRepo.GetUserRoleIdAsync(caller.Id);
        if (callerRole < 4) return Forbid(); // Super Admin only can change roles directly

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        try 
        {
            var userRole = new UserRole
            {
                UserId = id,
                RoleId = (int)dto.RoleId,
                AssignedBy = caller.Id,
                AssignedAt = DateTime.UtcNow
            };

            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Role updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpPut("{id}/approval")]
    public async Task<IActionResult> UpdateApproval(long id, [FromBody] UpdateApprovalDto dto)
    {
        var caller = await GetCurrentUserAsync();
        if (caller == null) return Unauthorized();

        var callerRole = await _userRepo.GetUserRoleIdAsync(caller.Id);
        if (callerRole < 3) return Forbid(); // Admin+ only

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        try 
        {
            if (!Enum.TryParse<ApprovalStatus>(dto.Status, true, out var status))
            {
                return BadRequest("Invalid approval status. Must be Pending, Approved, or Rejected.");
            }
            user.ApprovalStatus = status;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Approval status updated" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(long id)
    {
        var caller = await GetCurrentUserAsync();
        if (caller == null) return Unauthorized();

        var callerRole = await _userRepo.GetUserRoleIdAsync(caller.Id);
        if (callerRole < 4) return Forbid(); // Super Admin only

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var sub = User.Subject();
        var provider = User.Provider();
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(provider)) return null;
        return await _userRepo.GetByIdentityAsync(provider, sub);
    }

    private async Task<string> GetRoleNameAsync(long userId)
    {
        var roleId = await _userRepo.GetUserRoleIdAsync(userId);
        return roleId switch
        {
            1 => "default_user",
            2 => "User",
            3 => "Admin",
            4 => "super_admin",
            _ => "User"
        };
    }
}

public record UpdateRoleDto(long RoleId);
public record UpdateApprovalDto(string Status);
