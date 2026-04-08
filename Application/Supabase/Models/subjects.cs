using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

[Table("subjects")]
public class Subject : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("course")]
    public int Course {get; set;} = 1;
}
