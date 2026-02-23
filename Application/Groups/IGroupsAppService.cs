public interface IGroupsAppService
{
    Task<IEnumerable<Group>> GetAllAsync();
    Task<Group?> GetByIdAsync(Guid id);
    Task<Group> CreateAsync(Group group);
    Task UpdateAsync(Group group);
    Task DeleteAsync(Guid id);
    Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid subjectId);
}
