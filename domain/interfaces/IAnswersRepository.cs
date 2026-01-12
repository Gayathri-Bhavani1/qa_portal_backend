using qa_portal_apis.domain.entities;

namespace qa_portal_apis.domain.interfaces;

public interface IAnswersRepository
{
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(long questionId);
    Task<Answer?> GetByIdAsync(long id);
    Task AddAsync(Answer answer);
    Task UpdateAsync(Answer answer);
    Task DeleteAsync(long id);
    Task SaveChangesAsync();
}
