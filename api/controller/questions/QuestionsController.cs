using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;
using qa_portal_apis.infrastructure.auth;
using qa_portal_apis.domain.enums;
using qa_portal_apis.infrastructure.persistence;

namespace qa_portal_apis.api.controller.questions;

[ApiController]
[Route("api/questions")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionsRepository _questionsRepo;
    private readonly IAnswersRepository _answersRepo;
    private readonly IUserRepository _userRepo;
    private readonly AppDbContext _db;

    public QuestionsController(
        IQuestionsRepository questionsRepo, 
        IAnswersRepository answersRepo,
        IUserRepository userRepo,
        AppDbContext db)
    {
        _questionsRepo = questionsRepo;
        _answersRepo = answersRepo;
        _userRepo = userRepo;
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var questions = await (from q in _db.Questions
                              join u in _db.Users on q.AuthorId equals u.Id
                              orderby q.CreatedAt descending
                              select new {
                                  q.Id,
                                  q.Title,
                                  q.Content,
                                  q.AuthorId,
                                  AuthorName = u.Name,
                                  q.IsEnded,
                                  q.CreatedAt,
                                  q.UpdatedAt,
                                  AnswerCount = _db.Answers.Count(a => a.QuestionId == q.Id)
                              }).ToListAsync();
            
        return Ok(questions);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        var question = await (from q in _db.Questions
                             join u in _db.Users on q.AuthorId equals u.Id
                             where q.Id == id
                             select new {
                                 q.Id,
                                 q.Title,
                                 q.Content,
                                 q.AuthorId,
                                 AuthorName = u.Name,
                                 q.IsEnded,
                                 q.CreatedAt,
                                 q.UpdatedAt
                             }).FirstOrDefaultAsync();

        if (question == null) return NotFound();
        return Ok(question);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateQuestionDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var roleId = await _userRepo.GetUserRoleIdAsync(user.Id);
        if (roleId < 2) 
        {
            return StatusCode(403, "Default users cannot post questions.");
        }

        if (user.ApprovalStatus != ApprovalStatus.Approved && roleId != (int)Role.SuperAdmin)
        {
            return StatusCode(403, "Your account is pending approval.");
        }



        var question = new Question
        {
            Title = dto.Title,
            Content = dto.Content,
            AuthorId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _questionsRepo.AddAsync(question);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(long id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var question = await _questionsRepo.GetByIdAsync(id);
        if (question == null) return NotFound();

        // Author, Admin, or Super Admin can delete
        var role = await _userRepo.GetUserRoleIdAsync(user.Id);
        if (question.AuthorId != user.Id && role < 3) // 3 = Admin
        {
            return Forbid();
        }

        await _questionsRepo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/end")]
    [Authorize]
    public async Task<IActionResult> EndConversation(long id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var role = await _userRepo.GetUserRoleIdAsync(user.Id);
        if (role < 3) return Forbid(); // Admin or Super Admin only

        var question = await _questionsRepo.GetByIdAsync(id);
        if (question == null) return NotFound();

        question.IsEnded = true;
        await _questionsRepo.UpdateAsync(question);
        return Ok(question);
    }

    [HttpGet("{id}/answers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAnswers(long id)
    {
        var answers = await (from a in _db.Answers
                            join u in _db.Users on a.AuthorId equals u.Id
                            where a.QuestionId == id
                            orderby a.CreatedAt
                            select new {
                                a.Id,
                                a.QuestionId,
                                a.Content,
                                a.AuthorId,
                                AuthorName = u.Name,
                                a.ParentId,
                                a.CreatedAt,
                                a.UpdatedAt
                            }).ToListAsync();
        return Ok(answers);
    }

    [HttpPost("{id}/answers")]
    [Authorize]
    public async Task<IActionResult> CreateAnswer(long id, [FromBody] CreateAnswerDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var roleId = await _userRepo.GetUserRoleIdAsync(user.Id);

        // Default users (RoleId 1) can only view
        if (roleId < 2) 
        {
            return StatusCode(403, "Default users cannot post comments.");
        }

        if (user.ApprovalStatus != ApprovalStatus.Approved && roleId != (int)Role.SuperAdmin)
        {
            return StatusCode(403, "Your account is pending approval.");
        }

        var question = await _questionsRepo.GetByIdAsync(id);
        if (question == null) return NotFound();
        if (question.IsEnded) return BadRequest("This conversation has ended.");

        var answer = new Answer
        {
            QuestionId = id,
            Content = dto.Content,
            AuthorId = user.Id,
            ParentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _answersRepo.AddAsync(answer);
        return Ok(answer);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var sub = User.Subject();
        var provider = User.Provider();
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(provider)) return null;
        return await _userRepo.GetByIdentityAsync(provider, sub);
    }
}

public record CreateQuestionDto(string Title, string Content);
public record CreateAnswerDto(string Content, long? ParentId = null);
public record UpdateAnswerDto(string Content);