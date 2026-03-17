public class SchedulesDto
{
    public Guid? Id { get; set; }
    public GroupsDto? Group { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public LocationDto? Location { get; set; }
}