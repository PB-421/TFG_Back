using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("control")]
public class Control : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; }
}
