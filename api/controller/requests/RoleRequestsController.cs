using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;
using qa_portal_apis.domain.enums;
using qa_portal_apis.infrastructure.auth;
using qa_portal_apis.infrastructure.persistence;

namespace qa_portal_apis.api.controller.requests;

[Authorize]
[ApiController]
[Route("api/role-requests")]
public class RoleRequestsController : ControllerBase
{
    private readonly IRoleRequestRepository _requestRepo;
    private readonly IUserRepository _userRepo;
    private readonly AppDbContext _db;

    public RoleRequestsController(IRoleRequestRepository requestRepo, IUserRepository userRepo, AppDbContext db)
    {
        _requestRepo = requestRepo;
        _userRepo = userRepo;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequests()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var roleId = await _userRepo.GetUserRoleIdAsync(user.Id);
        
        if (roleId >= 4) // Super Admin
        {
            return Ok(await _requestRepo.GetAllAsync());
        }
        
        return Ok(await _requestRepo.GetByUserIdAsync(user.Id));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateRoleRequestDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var currentRoleId = await _userRepo.GetUserRoleIdAsync(user.Id);

        var request = new RoleChangeRequest
        {
            UserId = user.Id,
            CurrentRoleId = currentRoleId,
            RequestedRoleId = dto.RequestedRoleId,
            Note = dto.Note,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(request);
        return Ok(request);
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> ReviewRequest(long id, [FromBody] ReviewRoleRequestDto dto)
    {
        var reviewer = await GetCurrentUserAsync();
        if (reviewer == null) return Unauthorized();

        var reviewerRoleId = await _userRepo.GetUserRoleIdAsync(reviewer.Id);
        if (reviewerRoleId < 4) return Forbid(); // Super Admin only

        var request = await _requestRepo.GetByIdAsync(id);
        if (request == null) return NotFound();

        try 
        {
            request.Status = dto.Approved ? "Approved" : "Rejected";
            request.ReviewComment = dto.Comment;
            request.ReviewerId = reviewer.Id;
            request.ReviewedAt = DateTime.UtcNow;

            await _requestRepo.UpdateAsync(request);

            if (dto.Approved)
            {
                // Update user role
                var userRole = new UserRole
                {
                    UserId = request.UserId,
                    RoleId = (int)request.RequestedRoleId,
                    AssignedBy = reviewer.Id,
                    AssignedAt = DateTime.UtcNow
                };
                await _userRepo.AddUserRoleAsync(userRole);

                // ALSO: Automatically approve the user's account if it's pending/rejected
                var userToApprove = await _db.Users.FindAsync(request.UserId);
                if (userToApprove != null)
                {
                    userToApprove.ApprovalStatus = ApprovalStatus.Approved;
                    userToApprove.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var sub = User.Subject();
        var provider = User.Provider();
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(provider)) return null;
        return await _userRepo.GetByIdentityAsync(provider, sub);
    }
}

public record CreateRoleRequestDto(long RequestedRoleId, string Note);
public record ReviewRoleRequestDto(bool Approved, string Comment);
