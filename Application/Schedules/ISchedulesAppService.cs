public interface ISchedulesAppService
{
    Task<IEnumerable<Schedule>> GetAllAsync();
    Task<Schedule?> GetByIdAsync(Guid id);
    Task<Schedule> CreateAsync(Schedule schedule);
    Task UpdateAsync(Schedule schedule);
    Task DeleteAsync(Guid id);
    Task<int> GetGroupCapacityByGroupId(Guid groupId);
}
