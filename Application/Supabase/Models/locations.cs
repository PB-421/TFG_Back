using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("locations")]
public class Location : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("capacity")]
    public int Capacity { get; set; }
}
