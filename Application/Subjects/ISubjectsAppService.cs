public interface ISubjectsAppService
{
    Task<List<SubjectDto>> GetAllAsync();
    Task<bool> CreateAsync(SubjectDto subject);
    Task<bool> UpdateAsync(SubjectDto subject);
    Task<bool> DeleteAsync(Guid id);
}
