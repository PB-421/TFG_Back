public class LocationsAppService : ILocationsAppService
{
    private readonly ISupabaseService<Location> _repository;

    public LocationsAppService(ISupabaseService<Location> repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<Location>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<Location?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public async Task<int> GetCapacityByIdAsync(Guid id)
    {
        var location = await _repository.GetByIdAsync(id);
        if(location != null) return location.Capacity;
        else return 0; 
    }

    public Task<Location> CreateAsync(Location location)
        => _repository.CreateAsync(location);

    public Task UpdateAsync(Location location)
        => _repository.UpdateAsync(location);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);
}
