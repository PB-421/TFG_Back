public interface IRequestsAppService
{
    Task<List<RequestDto>> GetAllAsync();
    Task<RequestDto> GetByIdAsync(Guid id);
    Task<bool> CreateAsync(RequestDto request);
    Task<bool> UpdateAsync(Guid id, RequestDto request);
    Task<bool> DeleteAsync(Guid id);
    Task<(bool ok, string? error)> ResolveWithMinCostFlowAsync();
    Task<(bool ok, string? error)> ApplyAcceptedRequestsAsync();
}
