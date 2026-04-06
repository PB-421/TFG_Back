public interface IRequestsAppService
{
    Task<List<RequestDto>> GetAllAsync();
    Task<List<RequestDto>> GetByStudentId(Guid StudentId);
    Task<RequestDto> GetByIdAsync(Guid id);
    Task<bool> CreateAsync(RequestDto request);
    Task<bool> UpdateAsync(Guid id, RequestDto request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteCompletedRequest();
}
