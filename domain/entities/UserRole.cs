using System.ComponentModel.DataAnnotations.Schema;

namespace qa_portal_apis.domain.entities;

public class UserRole
{
    public long Id { get; set; }
    
    public long UserId { get; set; }
    
    public int RoleId { get; set; }
    
    public long? AssignedBy { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
