using Supabase;
public class RequestsAppService : IRequestsAppService
{
    private readonly Client _client;

    private readonly IGroupsAppService _groupsRepo;
    public RequestsAppService(Client client, IGroupsAppService groupsRepo)
    {
        _client = client;
        _groupsRepo = groupsRepo;
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
            PdfPath = r.PdfPath,
            ManagedBy = r.ManagedBy
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
            PdfPath = r.PdfPath,
            ManagedBy = r.ManagedBy
        }).ToList();
    }

    public async Task<List<RequestDto>> GetByTeacherId(Guid TeacherId)
    {
        var groups = await _groupsRepo.GetTeacherGroupsbyTeacherId(TeacherId);
        var groupIds = groups.Select(g => g.Id).ToList();
        var response = await _client.From<Request>().Where(r => r.Status == 0 || r.Status == 3).Get();

        return response.Models
        .Where(r => groupIds.Contains(r.OriginGroupId) || groupIds.Contains(r.DestinationGroupId)) 
        .Select(r => new RequestDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            OriginGroupId = r.OriginGroupId,
            DestinationGroupId = r.DestinationGroupId,
            Weight = r.Weight,
            StudentComment = r.StudentComment,
            TeacherComment = r.TeacherComment,
            Status = r.Status,
            PdfPath = r.PdfPath,
            ManagedBy = r.ManagedBy
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
            PdfPath = result.PdfPath,
            ManagedBy = result.ManagedBy
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
            PdfPath = request.PdfPath,
            ManagedBy = request.ManagedBy
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
        if (current.ManagedBy != request.ManagedBy) {current.ManagedBy = request.ManagedBy; hasChanges = true;}

        if (!hasChanges)
            return false;

        await _client.From<Request>().Update(current);
        return true;
    }

    public async Task<bool> UpdateFromTeacherAsync(Guid id, RequestUpdateDto request)
    {
        var response = await _client
            .From<Request>()
            .Where(r => r.Id == id)
            .Get();

        var current = response.Models.FirstOrDefault();

        if (current == null)
            return false;
            
        bool hasChanges = false;

        if (current.Status != request.Status) { current.Status = request.Status ?? 0; hasChanges = true; }
        if (current.TeacherComment != request.TeacherComment) { current.TeacherComment = request.TeacherComment; hasChanges = true; }
        if (current.ManagedBy != request.ManagedBy) {current.ManagedBy = request.ManagedBy; hasChanges = true;}

        if (!hasChanges)
            return false;

        await _client.From<Request>().Update(current);
        if(request.Status == 2)
        {
            var groups = await _groupsRepo.GetAllAsync();

            var modifiedGroupIds = new HashSet<Guid>();

            var actualRequest = new RequestDto
            {
                Id = current.Id,
                StudentId = current.StudentId,
                OriginGroupId = current.OriginGroupId,
                DestinationGroupId = current.DestinationGroupId,
                Weight = current.Weight,
                StudentComment = current.StudentComment,
                TeacherComment = current.TeacherComment,
                Status = current.Status,
                PdfPath = current.PdfPath,
                ManagedBy = current.ManagedBy
            };


            var origin = groups.FirstOrDefault(g => g.Id == actualRequest.OriginGroupId);
            var dest = groups.FirstOrDefault(g => g.Id == actualRequest.DestinationGroupId);

            if (origin != null && dest != null)
            {
                // --- 1. Remover del origen ---
                if (origin.Students != null && origin.Students.Any(s => s.Id == actualRequest.StudentId))
                {
                    origin.Students = origin.Students.Where(s => s.Id != actualRequest.StudentId).ToList();
                    modifiedGroupIds.Add(origin.Id!.Value);
                }

                // --- 2. Agregar al destino ---
                if (dest.Students != null && !dest.Students.Any(s => s.Id == actualRequest.StudentId))
                {
                    dest.Students.Add(new profileDto { Id = actualRequest.StudentId });
                    modifiedGroupIds.Add(dest.Id!.Value);
                }
            }
            
            foreach (var groupId in modifiedGroupIds)
            {
                var groupDto = groups.First(g => g.Id == groupId);
                await _groupsRepo.UpdateAsync(groupId, groupDto);
            }
        }
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
