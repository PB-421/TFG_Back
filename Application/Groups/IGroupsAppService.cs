public interface IGroupsAppService
{
    Task<List<GroupsDto>> GetAllAsync();
    Task<GroupsDto> GetById(Guid? id);
    Task<List<GroupsDto>> GetStudentGroupsByIdAsync(Guid studentId);
    Task<List<GroupsDto>> GetTeacherGroupsbyTeacherId(Guid? teacherId);
    Task<List<GroupsDto>> GetSameCourseGroups(Guid? subjectId);
    Task<bool> CreateAsync(GroupsDto dto);
    Task<GroupsDto> GetGroupsNamesByIds(Guid id);
    Task<bool> UpdateAsync(Guid id, GroupsDto dto, bool algorithm = false);
    Task<bool> DeleteAsync(Guid id);
}
