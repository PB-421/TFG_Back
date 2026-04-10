public interface ISchedulesAppService
{
    Task<List<SchedulesDto>> GetAllAsync();
    Task<List<SchedulesDto>> GetSchedulesByGroupIdAsync(Guid? groupId);
    Task<bool> CreateAsync(SchedulesDto dto);
    Task<List<Guid>> GetLocationsById(Guid? groupId);
    Task<bool> LocationInUse(Guid? LocationId);
    Task<bool> UpdateAsync(Guid id, SchedulesDto dto);
    Task<bool> DeleteAsync(Guid id);
}
