public class AlgorithmsAppService : IAlgorithmsAppService
{
    private readonly IProfilesAppService _userRepo;
    private readonly ISchedulesAppService _schedulesRepo;
    private readonly IGroupsAppService _groupsService;
    private readonly ILocationsAppService _locationsService;

    public AlgorithmsAppService(IProfilesAppService userRepo, ISchedulesAppService schedulesRepo, IGroupsAppService groupsService, ILocationsAppService locationService)
    {
        _userRepo = userRepo;
        _schedulesRepo = schedulesRepo;
        _groupsService = groupsService;
        _locationsService = locationService;
    }

    public async Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid? subjectId)
    {
        if(subjectId == null) return (false, "Id no valido");
        var groups = (await _groupsService.GetAllAsync())
            .Where(g => g.SubjectId == subjectId)
            .ToList();

        if (!groups.Any())
            return (false, "No hay grupos para esta asignatura");

        // Obtenemos todos los perfiles y filtramos por rol y asignatura
        var allStudents = (await _userRepo.GetAllProfilesInternaly())
            .Select(p => new profileDto 
            {
                Id = p.Id,
                Email = p.Email,
                Name = p.Name,
                Role = p.Role,
                Subjects = p.Subjects?.Select(s => new SubjectDto { 
                    Id = s.Id, 
                    Name = s.Name 
                }).ToList() ?? new List<SubjectDto>()
            })
            .Where(p => p.Role == "student" && p.Subjects.Any(s => s.Id == subjectId))
            .ToList();

        if (!allStudents.Any())
            return (false, "No hay alumnos matriculados en la asignatura");

        var assignedStudentIds = groups
            .SelectMany(g => g.Students ?? new List<profileDto>())
            .Select(p => p.Id)
            .Distinct()
            .ToHashSet();

        var unassignedStudents = allStudents
            .Where(s => !assignedStudentIds.Contains(s.Id))
            .ToList();

        if (!unassignedStudents.Any())
            return (true, null);

        var freeCapacity = new Dictionary<Guid, int>();
        foreach (var group in groups)
        {
            var groupLocations = await _schedulesRepo.GetLocationsById(group.Id!.Value);
            var totalCapacity = await _locationsService.GetLocationsCapacityByIds(groupLocations);
            var used = group.Students?.Count ?? 0;
            freeCapacity[group.Id.Value] = Math.Max(0, totalCapacity - used);
        }

        if (freeCapacity.Values.Sum() < unassignedStudents.Count)
            return (false, "No hay suficientes plazas para todos los alumnos");

        var groupBuckets = groups.ToDictionary(
            g => g.Id!.Value,
            g => g.Students ?? new List<profileDto>()
        );

        int index = 0;
        int totalGroups = groups.Count;

        foreach (var student in unassignedStudents)
        {
            int attempts = 0;
            while (attempts < totalGroups)
            {
                var group = groups[index];
                var groupId = group.Id!.Value;

                if (freeCapacity[groupId] > 0)
                {
                    groupBuckets[groupId].Add(student);
                    freeCapacity[groupId]--;
                    index = (index + 1) % totalGroups;
                    break;
                }
                index = (index + 1) % totalGroups;
                attempts++;
            }
        }

        foreach (var group in groups)
        {
            group.Students = groupBuckets[group.Id!.Value];
            await _groupsService.UpdateAsync(group.Id!.Value,group,true);
        }

        return (true, null);
    }

}