public interface ILocationsAppService
{
    Task<List<LocationDto>> GetAllAsync();
    Task<LocationDto> GetLocationById(Guid? id);
    Task<int> GetLocationsCapacityByIds(List<Guid> Ids);
    Task<bool> CreateAsync(LocationDto location);
    Task<bool> UpdateAsync(Guid id, LocationDto location);
    Task<bool> DeleteAsync(Guid id);
}
