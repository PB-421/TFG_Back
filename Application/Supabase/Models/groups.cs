using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("groups")]
public class Group : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("teacher_id")]
    public Guid TeacherId { get; set; }

    [Column("students")]
    public Guid[] Students { get; set; } = [];
}
