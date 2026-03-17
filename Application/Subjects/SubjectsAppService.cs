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
            Name = s.Name 
        }).ToList();
    }

    public async Task<List<SubjectDto>> GetSubjectNamesByIds(List<Guid> ids)
    {
        if (ids == null || !ids.Any()) return new List<SubjectDto>();

        var response = await _client.From<Subject>()
            .Where(s => ids.Contains(s.Id))
            .Get();

        return response.Models
        .Select(s => new SubjectDto 
        { 
            Id = s.Id, 
            Name = s.Name 
        })
        .ToList();
    }
    public async Task<bool> CreateAsync(SubjectDto subject)
    {
        var newSubject = new Subject
        {
            Id= Guid.NewGuid(),
            Name = subject.Name!
        };
        await _client.From<Subject>().Insert(newSubject);

        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, SubjectDto subject)
    {
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
