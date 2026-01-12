namespace qa_portal_apis.domain.entities;

public class RoleChangeRequest
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long CurrentRoleId { get; set; }

    public long RequestedRoleId { get; set; }

    public string Status { get; set; } = "Pending"; // or enum mapping

    public string? Note { get; set; }

    public long? ReviewerId { get; set; }

    public string? ReviewComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}