public class RequestUpdateDto
{
    public Guid? Id { get; set; } = Guid.NewGuid();
    public Guid? StudentId { get; set; } = Guid.Empty;
    public Guid? OriginGroupId { get; set; }  = Guid.Empty;
    public Guid? DestinationGroupId { get; set; }  = Guid.Empty;
    public int? Weight { get; set; } = 0;
    public string? StudentComment { get; set; } = string.Empty;
    public string? TeacherComment { get; set; } = string.Empty;
    public int? Status { get; set; } = 0;
    public string? PdfPath {get; set;} = string.Empty;
}
