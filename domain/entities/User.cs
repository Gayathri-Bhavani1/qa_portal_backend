using qa_portal_apis.domain.enums;

namespace qa_portal_apis.domain.entities;

public class User
{
    public long Id { get; set; }
    
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string IdpProvider { get; set; } = default!;
    public string IdpSubjectId { get; set; } = default!;
    
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public Role Role { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    
    

}