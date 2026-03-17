public interface IAlgorithmsAppService
{
    Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid subjectId);
}