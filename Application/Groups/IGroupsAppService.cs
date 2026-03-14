public interface IGroupsAppService
{
    Task<List<GroupsDto>> GetAllAsync();
    Task<bool> CreateAsync(GroupsDto dto);
    Task<bool> UpdateAsync(Guid id, GroupsDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid subjectId);
}
