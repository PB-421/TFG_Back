public interface IGroupsAppService
{
    Task<List<GroupsDto>> GetAllAsync();
    Task<bool> CreateAsync(GroupsDto dto);
    Task<GroupsDto> GetGroupsNamesByIds(Guid id);
    Task<bool> UpdateAsync(Guid id, GroupsDto dto);
    Task<bool> DeleteAsync(Guid id);
}
