public interface ILocationsAppService
{
    Task<IEnumerable<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(Guid id);
    Task<int> GetCapacityByIdAsync(Guid id);
    Task<Location> CreateAsync(Location location);
    Task UpdateAsync(Location location);
    Task DeleteAsync(Guid id);
}
