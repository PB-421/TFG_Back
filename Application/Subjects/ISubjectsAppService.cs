public interface ISubjectsAppService
{
    Task<IEnumerable<Subject>> GetAllAsync();
    Task<Subject?> GetByIdAsync(Guid id);
    Task<Subject> CreateAsync(Subject subject);
    Task UpdateAsync(Subject subject);
    Task DeleteAsync(Guid id);
}
