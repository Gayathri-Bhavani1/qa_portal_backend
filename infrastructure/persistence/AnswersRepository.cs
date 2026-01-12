using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;

namespace qa_portal_apis.infrastructure.persistence;

public class AnswersRepository : IAnswersRepository
{
    private readonly AppDbContext _db;

    public AnswersRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(long questionId) =>
        await _db.Answers.Where(a => a.QuestionId == questionId).OrderBy(a => a.CreatedAt).ToListAsync();

    public async Task<Answer?> GetByIdAsync(long id) =>
        await _db.Answers.FindAsync(id);

    public async Task AddAsync(Answer answer)
    {
        _db.Answers.Add(answer);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Answer answer)
    {
        _db.Answers.Update(answer);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var answer = await _db.Answers.FindAsync(id);
        if (answer != null)
        {
            _db.Answers.Remove(answer);
            await _db.SaveChangesAsync();
        }
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
