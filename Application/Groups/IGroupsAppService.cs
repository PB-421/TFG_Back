public interface IGroupsAppService
{
    Task<List<GroupsDto>> GetAllAsync();
    Task<List<GroupsDto>> GetStudentGroupsByIdAsync(Guid studentId);
    Task<bool> CreateAsync(GroupsDto dto);
    Task<GroupsDto> GetGroupsNamesByIds(Guid id);
    Task<bool> UpdateAsync(Guid id, GroupsDto dto);
    Task<bool> DeleteAsync(Guid id);
}
