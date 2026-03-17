using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("schedules")]
public class Schedule : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("group_id")]
    public Guid GroupId { get; set; } = Guid.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("location_id")]
    public Guid LocationId { get; set; }  = Guid.Empty;
}
