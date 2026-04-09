public interface IRequestsAppService
{
    Task<List<RequestDto>> GetAllAsync();
    Task<List<RequestDto>> GetByStudentId(Guid StudentId);
    Task<List<RequestDto>> GetByTeacherId(Guid TeacherId);
    Task<RequestDto> GetByIdAsync(Guid id);
    Task<bool> CreateAsync(RequestDto request);
    Task<bool> UpdateAsync(Guid id, RequestDto request);
    Task<bool> UpdateFromTeacherAsync(Guid id, RequestUpdateDto request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteCompletedRequest();
}
