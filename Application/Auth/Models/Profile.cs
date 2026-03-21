using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("role")]
    public string Role { get; set; } = "";

    [Column("subjects")]
    public Guid[] Subjects {get; set;} = Array.Empty<Guid>();
}
