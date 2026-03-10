using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("locations")]
public class Location : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("capacity")]
    public int Capacity { get; set; }
}
