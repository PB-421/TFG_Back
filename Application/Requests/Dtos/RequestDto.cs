public class RequestDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid OriginGroupId { get; set; }
    public Guid DestinationGroupId { get; set; }
    public int Weight { get; set; }
    public string? StudentComment { get; set; }
    public string? TeacherComment { get; set; }
    public int Status { get; set; }
    public string? PdfPath {get; set;}
}
