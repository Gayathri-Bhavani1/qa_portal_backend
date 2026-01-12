using System.ComponentModel.DataAnnotations.Schema;

namespace qa_portal_apis.domain.entities;

[Table("answers")]
public class Answer
{
    [Column("id")]
    public long Id { get; set; }
    
    [Column("question_id")]
    public long QuestionId { get; set; }
    
    [Column("body")]
    public string Content { get; set; } = default!;
    
    [Column("user_id")]
    public long AuthorId { get; set; }
    
    [Column("parent_id")]
    public long? ParentId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
