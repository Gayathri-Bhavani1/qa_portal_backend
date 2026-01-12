using qa_portal_apis.domain.entities;

namespace qa_portal_apis.domain.interfaces;

public interface IRoleRequestRepository
{
    Task<IEnumerable<RoleChangeRequest>> GetAllAsync();
    Task<IEnumerable<RoleChangeRequest>> GetByUserIdAsync(long userId);
    Task<RoleChangeRequest?> GetByIdAsync(long id);
    Task AddAsync(RoleChangeRequest request);
    Task UpdateAsync(RoleChangeRequest request);
}
