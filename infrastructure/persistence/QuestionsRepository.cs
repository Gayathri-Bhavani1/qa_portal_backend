using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.entities;
using qa_portal_apis.domain.interfaces;

namespace qa_portal_apis.infrastructure.persistence;

public class QuestionsRepository : IQuestionsRepository
{
    private readonly AppDbContext _db;

    public QuestionsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Question>> GetAllAsync() =>
        await _db.Questions.OrderByDescending(q => q.CreatedAt).ToListAsync();

    public async Task<Question?> GetByIdAsync(long id) =>
        await _db.Questions.FindAsync(id);

    public async Task AddAsync(Question question)
    {
        _db.Questions.Add(question);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Question question)
    {
        _db.Questions.Update(question);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var question = await _db.Questions.FindAsync(id);
        if (question != null)
        {
            _db.Questions.Remove(question);
            await _db.SaveChangesAsync();
        }
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
