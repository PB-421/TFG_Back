using System.Text.Json;

public class SubjectsAppService : ISubjectsAppService
{
    private readonly Supabase.Client _client;

    public SubjectsAppService(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<List<SubjectDto>> GetAllAsync()
    {
        var response = await _client.From<Subject>().Select("*").Get();
    
        return response.Models.Select(s => new SubjectDto 
        { 
            Id = s.Id, 
            Name = s.Name,
            Course = s.Course 
        }).ToList();
    }

    public async Task<SubjectDto?> GetSubjectById(Guid? id)
    {
        if(id == null) return new SubjectDto();

        var result = await _client
            .From<Subject>()
            .Select("*")
            .Where(p => p.Id == id)
            .Single();
        if(result == null) return null;
        var subject = new SubjectDto
        {
            Id = result.Id,
            Name = result.Name,
            Course = result.Course
        };

        return subject;
    }

    public async Task<List<SubjectDto>> GetSubjectNamesByIds(List<Guid> ids)
    {
        if (ids == null || !ids.Any()) return new List<SubjectDto>();

        var response = await _client.From<Subject>()
            .Select("*")
            .Get();

        return response.Models
        .Where(s => ids.Contains(s.Id))   // filtrado en memoria
        .Select(s => new SubjectDto { Id = s.Id, Name = s.Name, Course=s.Course })
        .ToList();
    }

    public async Task<List<Guid>> GetSameCourseSubjectsBySubjectId(Guid? id)
    {
        if (id == null) return new List<Guid>();

        List<Guid> ids = new List<Guid>();
        var subject = await GetSubjectById(id);

        if(subject == null) new List<Guid>();

        var response = await _client.From<Subject>()
            .Select("*")
            .Where(s => s.Course == subject!.Course)
            .Get();

        return response.Models
        .Select(s => s.Id)
        .ToList();
    }

    public async Task<bool> ExistByName(string Name)
    {
        var response = await _client
            .From<Subject>()
            .Where(s => s.Name == Name)
            .Get();

        if (response == null) return false;
        if (response.Models == null || !response.Models.Any()) return false;
        return true;
    }
    public async Task<bool> CreateAsync(SubjectDto subject)
    {
        if(await ExistByName(subject.Name!)) return false;
        if(subject.Course == null) return false;
        var newSubject = new Subject
        {
            Id= Guid.NewGuid(),
            Name = subject.Name!,
            Course = subject.Course ?? 1
        };
        await _client.From<Subject>().Insert(newSubject);

        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, SubjectDto subject)
    {
        if(await ExistByName(subject.Name!)) return false;
        var currentSubject = await _client
            .From<Subject>()
            .Where(s => s.Id == id)
            .Single();
        
        if (currentSubject == null)
                return false;
        bool hasChanges = false;

        if (!string.IsNullOrWhiteSpace(subject.Name) && currentSubject.Name != subject.Name)
        {
            currentSubject.Name = subject.Name;
            hasChanges = true;
        }

        if (subject.Course != null && currentSubject.Course != subject.Course)
        {
            currentSubject.Course = subject.Course ?? 1;
            hasChanges = true;
        }

        if (!hasChanges)
                return false;

        await _client
            .From<Subject>()
            .Update(currentSubject);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _client.From<Subject>().Where(s => s.Id == id).Delete();
        return true;
    }
       
}
