public class GroupsDto
{
    public Guid? Id { get; set; }
    public Guid? SubjectId { get; set; }
    public string? Name { get; set; }
    public Guid? TeacherId { get; set; }
    public List<profileDto>? Students { get; set; }
}