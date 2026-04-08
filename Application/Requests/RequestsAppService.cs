using Supabase;
public class RequestsAppService : IRequestsAppService
{
    private readonly Client _client;

    private readonly IGroupsAppService _groupsRepo;
    private readonly ILocationsAppService _locationService;
    private readonly ISchedulesAppService _schedulesRepo;
    public RequestsAppService(Client client, IGroupsAppService groupsRepo, ISchedulesAppService schedulesRepo, ILocationsAppService locationService)
    {
        _client = client;
        _groupsRepo = groupsRepo;
        _schedulesRepo = schedulesRepo;
        _locationService = locationService;
    }

    public async Task<List<RequestDto>> GetAllAsync()
    {
        var response = await _client.From<Request>().Select("*").Get();

        return response.Models.Select(r => new RequestDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            OriginGroupId = r.OriginGroupId,
            DestinationGroupId = r.DestinationGroupId,
            Weight = r.Weight,
            StudentComment = r.StudentComment,
            TeacherComment = r.TeacherComment,
            Status = r.Status,
            PdfPath = r.PdfPath
        }).ToList();
    }

    public async Task<List<RequestDto>> GetByStudentId(Guid StudentId)
    {
        var response = await _client.From<Request>().Select("*").Where(r => r.StudentId == StudentId).Get();

        return response.Models.Select(r => new RequestDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            OriginGroupId = r.OriginGroupId,
            DestinationGroupId = r.DestinationGroupId,
            Weight = r.Weight,
            StudentComment = r.StudentComment,
            TeacherComment = r.TeacherComment,
            Status = r.Status,
            PdfPath = r.PdfPath
        }).ToList();
    }

    public async Task<RequestDto> GetByIdAsync(Guid id)
    {
        var result = await _client
            .From<Request>()
            .Select("*")
            .Where(r => r.Id == id)
            .Single();

        if (result == null) return new RequestDto();

        return new RequestDto
        {
            Id = result.Id,
            StudentId = result.StudentId,
            OriginGroupId = result.OriginGroupId,
            DestinationGroupId = result.DestinationGroupId,
            Weight = result.Weight,
            StudentComment = result.StudentComment,
            TeacherComment = result.TeacherComment,
            Status = result.Status,
            PdfPath = result.PdfPath
        };
    }

    public async Task<bool> studentHasGroupRequest(Guid studentId, Guid OriginGroupId)
    {
        var existing = await _client
            .From<Request>()
            .Where(r => r.StudentId == studentId)
            .Where(r => r.Status == 0)
            .Where(r => r.OriginGroupId == OriginGroupId)
            .Get();

        if (existing.Models.Any())
            return true;
        
        return false;
    }

    public async Task<bool> CreateAsync(RequestDto request)
    {
        if(await studentHasGroupRequest(request.StudentId, request.OriginGroupId)) return false;

        var newRequest = new Request
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            OriginGroupId = request.OriginGroupId,
            DestinationGroupId = request.DestinationGroupId,
            Weight = request.Weight,
            StudentComment = request.StudentComment,
            TeacherComment = request.TeacherComment,
            Status = request.Status,
            PdfPath = request.PdfPath
        };

        await _client.From<Request>().Insert(newRequest);
        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, RequestDto request)
    {
        var response = await _client
            .From<Request>()
            .Where(r => r.Id == id)
            .Get();

        var current = response.Models.FirstOrDefault();

        if (current == null)
            return false;
            
        bool hasChanges = false;

        if (current.Status != request.Status) { current.Status = request.Status; hasChanges = true; }
        if (current.TeacherComment != request.TeacherComment) { current.TeacherComment = request.TeacherComment; hasChanges = true; }
        if (current.Weight != request.Weight) { current.Weight = request.Weight; hasChanges = true; }

        if (!hasChanges)
            return false;

        await _client.From<Request>().Update(current);
        return true;
    }

    private string GetRelativePathFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return String.Empty;
        
        const string bucketName = "RequestsPdfs/";
        var index = url.IndexOf(bucketName);
        if (index != -1)
        {
            return url.Substring(index + bucketName.Length);
        }
        return String.Empty;
    }

    private async Task DeletePdfFromStorage(string pdfPath)
    {
        var relativePath = GetRelativePathFromUrl(pdfPath);
        if (!string.IsNullOrEmpty(relativePath))
        {
            try 
            {
                await _client.Storage
                    .From("RequestsPdfs")
                    .Remove(new List<string> { relativePath });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error borrando archivo de storage: {ex.Message}");
            }
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var request = await GetByIdAsync(id);
        if (request == null || request.Id == Guid.Empty) return false;

        if (!string.IsNullOrEmpty(request.PdfPath))
        {
            await DeletePdfFromStorage(request.PdfPath);
        }

        await _client.From<Request>().Where(r => r.Id == id).Delete();
        return true;
    }

    public async Task<bool> DeleteCompletedRequest()
    {
        var response = await _client
            .From<Request>()
            .Select("id, pdf_path") 
            .Where(r => r.Status == 2 || r.Status == 1)
            .Get();

        var requests = response.Models;

        foreach (var request in requests)
        {
            if (!string.IsNullOrEmpty(request.PdfPath))
            {
                await DeletePdfFromStorage(request.PdfPath);
            }
        
            await _client.From<Request>().Where(r => r.Id == request.Id).Delete();
        }
        return true;
    }
}
