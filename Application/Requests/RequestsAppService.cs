
public class RequestsAppService : IRequestsAppService
{
    private readonly ISupabaseService<Request> _repository;

    private readonly ISupabaseService<Group> _groupsRepo;

    private readonly ISchedulesAppService _schedulesRepo;
    public RequestsAppService(ISupabaseService<Request> repository, ISupabaseService<Group> groupsRepo, ISchedulesAppService schedulesRepo)
    {
        _repository = repository;
        _groupsRepo = groupsRepo;
        _schedulesRepo = schedulesRepo;
    }

    public Task<IEnumerable<Request>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<Request?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<Request> CreateAsync(Request request)
        => _repository.CreateAsync(request);

    public Task UpdateAsync(Request request)
        => _repository.UpdateAsync(request);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);

    public async Task<(bool ok, string? error)> ResolveWithMinCostFlowAsync()
    {
        var requests = (await _repository.GetAllAsync())
            .Where(r => r.Status == 0)
            .ToList();

        if (!requests.Any())
            return (true, null);

        var groups = (await _groupsRepo.GetAllAsync()).ToList();

        var freeCapacity = new Dictionary<Guid, int>();

        foreach (var group in groups)
        {
            // Capacidad total de la ubicación
            var totalCapacity = 0; 
            //var totalCapacity = await _schedulesRepo.GetGroupCapacityByGroupId(group.Id);

            // Alumnos ya asignados
            var currentStudents = group.Students?.Length ?? 0;

            // Capacidad libre real
            freeCapacity[group.Id] = Math.Max(0, totalCapacity - currentStudents);
        }

        int source = 0;
        int reqStart = 1;
        int grpStart = reqStart + requests.Count;
        int sink = grpStart + groups.Count;

        var mcmf = new MinCostMaxFlow(sink + 1);

        for (int i = 0; i < requests.Count; i++)
            mcmf.AddEdge(source, reqStart + i, 1, 0);

        for (int i = 0; i < requests.Count; i++)
        {
            var req = requests[i];
            int gIndex = groups.FindIndex(g => g.Id == req.DestinationGroupId);

            if (gIndex >= 0)
                mcmf.AddEdge(reqStart + i, grpStart + gIndex, 1, -req.Weight);
        }

        for (int i = 0; i < groups.Count; i++)
            mcmf.AddEdge(grpStart + i, sink, freeCapacity[groups[i].Id], 0);

        var (flow, _) = mcmf.GetMinCostMaxFlow(source, sink);

        for (int i = 0; i < requests.Count; i++)
        {
            requests[i].Status = i < flow ? 1 : 2;
            await _repository.UpdateAsync(requests[i]);
        }

        return (true, null);
    }

    // 2️⃣ Aplica los cambios reales
    public async Task<(bool ok, string? error)> ApplyAcceptedRequestsAsync()
    {
        var accepted = (await _repository.GetAllAsync())
            .Where(r => r.Status == 1)
            .ToList();

        var groups = (await _groupsRepo.GetAllAsync()).ToList();

        foreach (var r in accepted)
        {
            var origin = groups.First(g => g.Id == r.OriginGroupId);
            var dest = groups.First(g => g.Id == r.DestinationGroupId);

            origin.Students = origin.Students.Where(s => s != r.StudentId).ToArray();

            if (!dest.Students.Contains(r.StudentId))
                dest.Students = dest.Students.Append(r.StudentId).ToArray();

            r.Status = 3;
            await _repository.UpdateAsync(r);
        }

        foreach (var g in groups)
            await _groupsRepo.UpdateAsync(g);

        return (true, null);
    }
}
