using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("schedules")]
public class Schedule : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("location_id")]
    public Guid LocationId { get; set; }
}
