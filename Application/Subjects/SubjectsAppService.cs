public class SubjectsAppService : ISubjectsAppService
{
    private readonly ISupabaseService<Subject> _repository;

    public SubjectsAppService(ISupabaseService<Subject> repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<Subject>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<Subject?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<Subject> CreateAsync(Subject subject)
        => _repository.CreateAsync(subject);

    public Task UpdateAsync(Subject subject)
        => _repository.UpdateAsync(subject);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);
}
