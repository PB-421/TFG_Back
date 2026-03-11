public class GroupsAppService : IGroupsAppService
{
    private readonly ISupabaseService<Group> _repository;
    private readonly IProfilesAppService _userRepo;
    private readonly ISchedulesAppService _schedulesRepo;
    


    public GroupsAppService(ISupabaseService<Group> repository, IProfilesAppService userRepo, ISchedulesAppService schedulesRepo)
    {
        _repository = repository;
        _userRepo = userRepo;
        _schedulesRepo = schedulesRepo;
    }

    public Task<IEnumerable<Group>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<Group?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<Group> CreateAsync(Group group)
        => _repository.CreateAsync(group);

    public Task UpdateAsync(Group group)
        => _repository.UpdateAsync(group);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);

    public async Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid subjectId)
    {
        // 1️⃣ Obtener grupos de la asignatura
        var groups = (await _repository.GetAllAsync())
            .Where(g => g.SubjectId == subjectId)
            .ToList();

        if (!groups.Any())
            return (false, "No hay grupos para esta asignatura");

        // 2️⃣ Obtener alumnos matriculados en la asignatura
        var allStudents = (await _userRepo.GetAllProfilesInternaly())
            .Where(p => p.Role == "student" && p.Subjects.Any(s => s.Id == subjectId))
            .Select(p => p.Id)
            .ToList();

        if (!allStudents.Any())
            return (false, "No hay alumnos matriculados en la asignatura");

        // 3️⃣ Alumnos ya asignados a algún grupo
        var assignedStudents = groups
            .SelectMany(g => g.Students ?? Array.Empty<Guid>())
            .Distinct()
            .ToHashSet();

        // 4️⃣ Alumnos sin grupo
        var unassignedStudents = allStudents
            .Where(s => !assignedStudents.Contains(s))
            .ToList();

        if (!unassignedStudents.Any())
            return (true, null); // nada que repartir

        // 5️⃣ Capacidad libre REAL por grupo
        var freeCapacity = new Dictionary<Guid, int>();

        foreach (var group in groups)
        {
            var totalCapacity = await _schedulesRepo.GetGroupCapacityByGroupId(group.Id);
            var used = group.Students?.Length ?? 0;

            freeCapacity[group.Id] = Math.Max(0, totalCapacity - used);
        }

        // Comprobar plazas totales suficientes
        if (freeCapacity.Values.Sum() < unassignedStudents.Count)
            return (false, "No hay suficientes plazas para todos los alumnos");

        // 6️⃣ Buckets iniciales (con alumnos ya asignados)
        var groupBuckets = groups.ToDictionary(
            g => g.Id,
            g => g.Students?.ToList() ?? new List<Guid>()
        );

        // 7️⃣ Round Robin respetando capacidad
        int index = 0;
        int totalGroups = groups.Count;

        foreach (var studentId in unassignedStudents)
        {
            int attempts = 0;

            while (attempts < totalGroups)
            {
                var group = groups[index];
                var groupId = group.Id;

                if (freeCapacity[groupId] > 0)
                {
                    groupBuckets[groupId].Add(studentId);
                    freeCapacity[groupId]--;
                    index = (index + 1) % totalGroups;
                    break;
                }

                index = (index + 1) % totalGroups;
                attempts++;
            }
        }

        // 8️⃣ Guardar cambios
        foreach (var group in groups)
        {
            group.Students = groupBuckets[group.Id].ToArray();
            await _repository.UpdateAsync(group);
        }

        return (true, null);
    }
}
