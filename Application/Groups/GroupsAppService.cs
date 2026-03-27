using Supabase;

public class GroupsAppService : IGroupsAppService
{
    private readonly Client _client;
    private readonly IProfilesAppService _userRepo;

    public GroupsAppService(Client client, IProfilesAppService userRepo)
    {
        _client = client;
        _userRepo = userRepo;
    }

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

    public async Task<GroupsDto> GetById(Guid id)
    {
        var result = await _client
            .From<Group>()
            .Select("*")
            .Where(p => p.Id == id)
            .Single();
        if(result == null) return new GroupsDto();
        var group = new GroupsDto
        {
            Id = result.Id,
            Name = result.Name,
            TeacherId = result.TeacherId
        };

        return group;
    }

public async Task<List<GroupsDto>> GetStudentGroupsByIdAsync(Guid studentId)
{
    if (studentId == Guid.Empty) return new List<GroupsDto>();
    var response = await _client
        .From<Group>()
        .Select("*")
        .Get();

    var allGroups = response.Models ?? new List<Group>();

    // Filtrar en memoria por studentId
    var groupsOfStudent = allGroups
        .Where(g => g.Students.Contains(studentId))
        .ToList();

    if (!groupsOfStudent.Any()) return new List<GroupsDto>();

    var tasks = groupsOfStudent.Select(async g => new GroupsDto
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

    public async Task<GroupsDto> GetGroupsNamesByIds(Guid id)
    {
        var result = await _client
            .From<Group>()
            .Select("*")
            .Where(p => p.Id == id)
            .Single();
        if(result == null) return new GroupsDto();
        return new GroupsDto
        { 
            Id = result.Id, 
            Name = result.Name 
        };
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
    public async Task<bool> UpdateAsync(Guid id, GroupsDto dto, bool algorithm = false)
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

            if(algorithm){
                // Comparación de IDs de estudiantes
                var newStudentIds = dto.Students?.Select(p => p.Id).ToArray() ?? Array.Empty<Guid>();
                if (!Enumerable.SequenceEqual(current.Students ?? Array.Empty<Guid>(), newStudentIds))
                {
                    current.Students = newStudentIds;
                    hasChanges = true;
                }
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

    public async Task<List<GroupsDto>> GetTeacherGroupsbyTeacherId(Guid? teacherId)
    {
        if(teacherId == null) return new List<GroupsDto>();
        var result = await _client
            .From<Group>()
            .Select("*")
            .Where(g => g.TeacherId == teacherId)
            .Get();

        var tasks = result.Models.Select(async g => new GroupsDto
        {
            Id = g.Id,
            Name = g.Name,
        });

        var dtoArray = await Task.WhenAll(tasks);
        return dtoArray.ToList();
    }
}