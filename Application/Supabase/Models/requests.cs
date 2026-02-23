using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;


[Table("requests")]
public class Request : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("origin_group_id")]
    public Guid OriginGroupId { get; set; }

    [Column("destination_group_id")]
    public Guid DestinationGroupId { get; set; }

    [Column("weight")]
    public int Weight { get; set; }

    [Column("student_comment")]
    public string? StudentComment { get; set; }

    [Column("teacher_comment")]
    public string? TeacherComment { get; set; }

    [Column("status")]
    public int Status { get; set; }
}
