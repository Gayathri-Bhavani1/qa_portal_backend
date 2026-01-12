using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;
using qa_portal_apis.infrastructure.auth;
using qa_portal_apis.domain.enums;

namespace qa_portal_apis.api.controller.questions;

[ApiController]
[Route("api/answers")]
public class AnswersController : ControllerBase
{
    private readonly IAnswersRepository _answersRepo;
    private readonly IUserRepository _userRepo;

    public AnswersController(IAnswersRepository answersRepo, IUserRepository userRepo)
    {
        _answersRepo = answersRepo;
        _userRepo = userRepo;
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAnswerDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var answer = await _answersRepo.GetByIdAsync(id);
        if (answer == null) return NotFound();

        var roleId = await _userRepo.GetUserRoleIdAsync(user.Id);
        
        // Admins (3+) can edit anything, Users (2) can edit their own
        if (answer.AuthorId != user.Id && roleId < 3)
        {
            return Forbid();
        }

        answer.Content = dto.Content;
        answer.UpdatedAt = DateTime.UtcNow;

        await _answersRepo.UpdateAsync(answer);
        return Ok(answer);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(long id)
    {
        try 
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var answer = await _answersRepo.GetByIdAsync(id);
            if (answer == null) return NotFound();

            var roleId = await _userRepo.GetUserRoleIdAsync(user.Id);
            
            // Admins (3+) can delete anything, Users (2) can delete their own
            if (answer.AuthorId != user.Id && roleId < 3)
            {
                return Forbid();
            }

            await _answersRepo.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error deleting answer {id}: {ex}");
            if (ex.InnerException != null) 
            {
                Console.WriteLine($"[INNER ERROR]: {ex.InnerException}");
            }
            return StatusCode(500, new { 
                message = "Could not delete comment.", 
                details = ex.Message,
                inner = ex.InnerException?.Message 
            });
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
