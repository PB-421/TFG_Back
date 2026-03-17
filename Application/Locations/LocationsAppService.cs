public class LocationsAppService : ILocationsAppService
{
    private readonly Supabase.Client _client;

    public LocationsAppService(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<List<LocationDto>> GetAllAsync()
    {
        var response = await _client.From<Location>().Select("*").Get();
    
        return response.Models.Select(l => new LocationDto 
        { 
            Id = l.Id, 
            Name = l.Name,
            Capacity = l.Capacity
        }).ToList();
    }

    public async Task<LocationDto> GetLocationById(Guid? id)
    {
        var result = await _client
            .From<Location>()
            .Select("*")
            .Where(p => p.Id == id)
            .Single();
        if(result == null) return new LocationDto();
        var location = new LocationDto
        {
            Id = result.Id,
            Name = result.Name,
            Capacity = result.Capacity
        };

        return location;
    }

    public async Task<int> GetLocationsCapacityByIds(List<Guid> Ids)
    {
        int totalCapacity = 0;
        foreach (var location in Ids)
        {
            if (location == Guid.Empty) continue;

            LocationDto locationCapacity = await GetLocationById(location);

            int capacity = locationCapacity.Capacity ?? 0;

            if (totalCapacity == 0 || capacity <= totalCapacity)
            {
                totalCapacity = capacity;
            }
        }
        return totalCapacity;
    }

    public async Task<bool> CreateAsync(LocationDto location)
    {
        var newLocation = new Location
        {
            Id = Guid.NewGuid(),
            Name = location.Name ?? string.Empty,
            Capacity = location.Capacity ?? 0
        };

        await _client.From<Location>().Insert(newLocation);
        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, LocationDto location)
    {
        var response = await _client
            .From<Location>()
            .Where(l => l.Id == id)
            .Get();
        
        var currentLocation = response.Models.FirstOrDefault();
        
        if (currentLocation == null)
            return false;

        bool hasChanges = false;

        if (!string.IsNullOrWhiteSpace(location.Name) && currentLocation.Name != location.Name)
        {
            currentLocation.Name = location.Name;
            hasChanges = true;
        }

        if (location.Capacity.HasValue && currentLocation.Capacity != location.Capacity.Value)
        {
            currentLocation.Capacity = location.Capacity.Value;
            hasChanges = true;
        }

        if (!hasChanges)
            return false;

        await _client
            .From<Location>()
            .Update(currentLocation);
            
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _client.From<Location>().Where(l => l.Id == id).Delete();
        return true;
    }
}