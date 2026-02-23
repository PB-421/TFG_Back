public interface IRequestsAppService
{
    Task<IEnumerable<Request>> GetAllAsync();
    Task<Request?> GetByIdAsync(Guid id);
    Task<Request> CreateAsync(Request request);
    Task UpdateAsync(Request request);
    Task DeleteAsync(Guid id);
    Task<(bool ok, string? error)> ResolveWithMinCostFlowAsync();
    Task<(bool ok, string? error)> ApplyAcceptedRequestsAsync();
}
