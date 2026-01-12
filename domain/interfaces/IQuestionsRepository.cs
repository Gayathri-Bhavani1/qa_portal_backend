using qa_portal_apis.domain.entities;

namespace qa_portal_apis.domain.interfaces;

public interface IQuestionsRepository
{
    Task<IEnumerable<Question>> GetAllAsync();
    Task<Question?> GetByIdAsync(long id);
    Task AddAsync(Question question);
    Task UpdateAsync(Question question);
    Task DeleteAsync(long id);
    Task SaveChangesAsync();
}
