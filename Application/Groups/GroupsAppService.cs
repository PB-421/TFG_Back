using Supabase;

public class GroupsAppService : IGroupsAppService
{
    private readonly Client _client;
    private readonly IProfilesAppService _userRepo;
    private readonly ISchedulesAppService _schedulesRepo;

    public GroupsAppService(Client client, IProfilesAppService userRepo, ISchedulesAppService schedulesRepo)
    {
        _client = client;
        _userRepo = userRepo;
        _schedulesRepo = schedulesRepo;
    }

    // Obtener todos los grupos y mapear a DTO (resuelve los profileDto desde sus Ids)
    public async Task<List<GroupsDto>> GetAllAsync()
    {
        var result = await _client
            .From<Group>()
            .Select("*")
            .Get();

        var tasks = result.Models.Select(async g => new GroupsDto
        {
            Id = g.Id,
            SubjectId = g.SubjectId,
            Name = g.Name,
            TeacherId = g.TeacherId,
            Students = await GetProfilesByIdsAsync(g.Students.ToList())
        });

        var dtoArray = await Task.WhenAll(tasks);
        return dtoArray.ToList();
    }

    // Crear un nuevo grupo a partir del DTO
    public async Task<bool> CreateAsync(GroupsDto dto)
    {
        try
        {
            var newGroup = new Group
            {
                Id = dto.Id ?? Guid.NewGuid(),
                SubjectId = dto.SubjectId ?? Guid.Empty,
                Name = dto.Name ?? string.Empty,
                TeacherId = dto.TeacherId ?? Guid.Empty,
                Students = []
            };

            await _client.From<Group>().Insert(newGroup);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Actualizar un grupo existente a partir del DTO
    public async Task<bool> UpdateAsync(Guid id, GroupsDto dto)
    {
        if (id == Guid.Empty)
            return false;

        try
        {
            var response = await _client
                .From<Group>()
                .Where(g => g.Id == id)
                .Get();

            var current = response.Models.FirstOrDefault();
            if (current == null) return false;

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.Name) && current.Name != dto.Name)
            {
                current.Name = dto.Name!;
                hasChanges = true;
            }

            if (dto.SubjectId != null && current.SubjectId != dto.SubjectId)
            {
                current.SubjectId = dto.SubjectId.Value;
                hasChanges = true;
            }

            if (dto.TeacherId != null && current.TeacherId != dto.TeacherId)
            {
                current.TeacherId = dto.TeacherId.Value;
                hasChanges = true;
            }

            // Comparación de IDs de estudiantes
            var newStudentIds = dto.Students?.Select(p => p.Id).ToArray() ?? Array.Empty<Guid>();
            if (!Enumerable.SequenceEqual(current.Students ?? Array.Empty<Guid>(), newStudentIds))
            {
                current.Students = newStudentIds;
                hasChanges = true;
            }

            if (!hasChanges) return false;

            await _client.From<Group>().Update(current);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _client.From<Group>().Where(g => g.Id == id).Delete();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<profileDto>> GetProfilesByIdsAsync(List<Guid> ids)
    {
        List<profileDto> students = [];
        foreach(var id in ids)
        {
            var student = await _userRepo.GetProfileById(id);
            if(student != null) students.Add(student);
        }
        return students;
    }
    
    public async Task<(bool ok, string? error)> DistributeStudentsRoundRobinAsync(Guid subjectId)
    {
        var groups = (await GetAllAsync())
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
                // Asegúrate de mapear también las asignaturas si es necesario
                Subjects = p.Subjects?.Select(s => new SubjectDto { 
                    Id = s.Id, 
                    Name = s.Name 
                }).ToList() ?? new List<SubjectDto>()
            })
            .Where(p => p.Role == "student" && p.Subjects.Any(s => s.Id == subjectId))
            .ToList();

        if (!allStudents.Any())
            return (false, "No hay alumnos matriculados en la asignatura");

        // Alumnos ya asignados (usando IDs)
        var assignedStudentIds = groups
            .SelectMany(g => g.Students ?? new List<profileDto>())
            .Select(p => p.Id)
            .Distinct()
            .ToHashSet();

        // Alumnos sin grupo (objetos completos para poder añadirlos luego)
        var unassignedStudents = allStudents
            .Where(s => !assignedStudentIds.Contains(s.Id))
            .ToList();

        if (!unassignedStudents.Any())
            return (true, null);

        var freeCapacity = new Dictionary<Guid, int>();
        foreach (var group in groups)
        {
            var totalCapacity = await _schedulesRepo.GetGroupCapacityByGroupId(group.Id!.Value);
            var used = group.Students?.Count ?? 0;
            freeCapacity[group.Id.Value] = Math.Max(0, totalCapacity - used);
        }

        if (freeCapacity.Values.Sum() < unassignedStudents.Count)
            return (false, "No hay suficientes plazas para todos los alumnos");

        // Diccionario para trabajar con los objetos profileDto
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
            await UpdateAsync(group.Id!.Value,group);
        }

        return (true, null);
    }
}