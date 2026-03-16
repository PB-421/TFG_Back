public class SchedulesAppService : ISchedulesAppService
{
    private readonly ISupabaseService<Schedule> _repository;

    private readonly ILocationsAppService _locationRepo;

    public SchedulesAppService(ISupabaseService<Schedule> repository, ILocationsAppService locationRepo)
    {
        _repository = repository;
        _locationRepo = locationRepo;
    }

    public Task<IEnumerable<Schedule>> GetAllAsync()
        => _repository.GetAllAsync();

    public async Task<int> GetGroupCapacityByGroupId(Guid groupId)
    {
        int totalCapacity = 0;
        var locations = (await _repository.GetAllAsync())
            .Where(g => g.GroupId == groupId)
            .Select(g => g.LocationId)
            .Distinct()
            .ToList();
        foreach (var location in locations)
        {
            LocationDto locationCapacity = await _locationRepo.GetLocationById(location);
            if(totalCapacity == 0)
            {
                totalCapacity = locationCapacity.Capacity ?? 0;
            } else if (locationCapacity.Capacity! <= totalCapacity)
            {
                totalCapacity = locationCapacity.Capacity ?? 0;
            }
        }
        return totalCapacity;
    }
    public Task<Schedule?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<Schedule> CreateAsync(Schedule schedule)
        => _repository.CreateAsync(schedule);

    public Task UpdateAsync(Schedule schedule)
        => _repository.UpdateAsync(schedule);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);
}
