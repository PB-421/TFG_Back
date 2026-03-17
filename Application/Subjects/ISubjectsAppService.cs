public interface ISubjectsAppService
{
    Task<List<SubjectDto>> GetAllAsync();
    Task<List<SubjectDto>> GetSubjectNamesByIds(List<Guid> ids);
    Task<bool> CreateAsync(SubjectDto subject);
    Task<bool> UpdateAsync(Guid id,SubjectDto subject);
    Task<bool> DeleteAsync(Guid id);
}
