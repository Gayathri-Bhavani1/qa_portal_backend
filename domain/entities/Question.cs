using System.ComponentModel.DataAnnotations.Schema;

namespace qa_portal_apis.domain.entities;

[Table("questions")]
public class Question
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = default!;
    
    [Column("body")]
    public string Content { get; set; } = default!;
    
    [Column("user_id")]
    public long AuthorId { get; set; }
    
    [Column("status")]
    public bool IsEnded { get; set; } = false;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
