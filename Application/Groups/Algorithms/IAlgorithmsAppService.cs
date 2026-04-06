public interface IAlgorithmsAppService
{
    Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid? subjectId);
    Task<(bool ok, string? error)> ResolveWithMinCostFlowAsync();
    Task<(bool ok, string? error)> ApplyAcceptedRequestsAsync();
}