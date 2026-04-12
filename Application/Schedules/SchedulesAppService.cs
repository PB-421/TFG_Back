using Supabase;
public class SchedulesAppService : ISchedulesAppService
{
    private readonly Client _client;

    private readonly ILocationsAppService _locationService;
    private readonly IGroupsAppService _groupsService;

    public SchedulesAppService(Client client, ILocationsAppService locationService, IGroupsAppService groupsService)
    {
        _client = client;
        _locationService = locationService;
        _groupsService = groupsService;
    }

    public async Task<List<SchedulesDto>> GetAllAsync()
    {
        var result = await _client
            .From<Schedule>()
            .Select("*")
            .Get();

        var tasks = result.Models.Select(async s => new SchedulesDto
        {
            Id = s.Id,
            Group = await _groupsService.GetGroupsNamesByIds(s.GroupId),
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Location = await _locationService.GetLocationById(s.LocationId)
        });

        var dtoArray = await Task.WhenAll(tasks);
        return dtoArray.ToList();
    }

    public async Task<List<SchedulesDto>> GetSchedulesByGroupIdAsync(Guid? groupId)
    {
        if(groupId == Guid.Empty || groupId == null) return new List<SchedulesDto>();
        var result = await _client
            .From<Schedule>()
            .Select("*")
            .Where(s => s.GroupId == groupId)
            .Get();

        var tasks = result.Models.Select(async s => new SchedulesDto
        {
            Id = s.Id,
            Group = await _groupsService.GetGroupsNamesByIds(s.GroupId),
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Location = await _locationService.GetLocationById(s.LocationId)
        });

        var dtoArray = await Task.WhenAll(tasks);
        return dtoArray.ToList();
    }

    public async Task<bool> CreateAsync(SchedulesDto dto)
    {
        if (!dto.StartDate.HasValue || !dto.EndDate.HasValue || dto.Location?.Id == null || dto.Group?.Id == null)
            throw new ArgumentException("Datos incompletos para crear el horario.");

        var locationOccupied = await IsLocationOccupiedAsync(dto.Location.Id.Value, dto.StartDate.Value, dto.EndDate.Value);
        if (locationOccupied) throw new InvalidOperationException($"LOCATION_OCCUPIED|{dto.Location.Name}|{dto.StartDate:dd/MM HH:mm}");


        var groupOccupied = await IsGroupOccupiedAsync(dto.Group.Id.Value, dto.StartDate.Value, dto.EndDate.Value);
        if (groupOccupied) throw new InvalidOperationException($"GROUP_OCCUPIED|{dto.StartDate:dd/MM HH:mm}");

        var teacherAvailable = await IsTeacherAvailable(dto.Group.Id.Value, dto.StartDate.Value, dto.EndDate.Value);
        if (!teacherAvailable) throw new InvalidOperationException($"TEACHER_OCCUPIED|{dto.StartDate:dd/MM HH:mm}");

        var subjectsConflic = await HasConflicWithSubjectsFromTheSameCourse(dto.Group.Id.Value, dto.StartDate.Value, dto.EndDate.Value);
        if (subjectsConflic) throw new InvalidOperationException($"SUBJECT_CONFLICT|{dto.StartDate:dd/MM HH:mm}");

        var newSchedule = new Schedule
        {
            Id = dto.Id ?? Guid.NewGuid(),
            GroupId = dto.Group.Id.Value,
            StartDate = dto.StartDate.Value,
            EndDate = dto.EndDate.Value,
            LocationId = dto.Location.Id.Value
        };

        await _client.From<Schedule>().Insert(newSchedule);
        return true;
    }

    public async Task<List<Guid>> GetLocationsById(Guid? groupId)
    {
        if(groupId == Guid.Empty || groupId == null) return new List<Guid>();
        var locations = (await GetAllAsync())
            .Where(g => g.Group!.Id == groupId)
            .Select(g => g.Location!.Id!.Value)
            .Distinct()
            .ToList();

        return locations;
    }

    public async Task<bool> LocationInUse(Guid? LocationId)
    {
        if(LocationId == null || LocationId == Guid.Empty) return false;
        var response = await _client
        .From<Schedule>()
        .Where(s => s.LocationId == LocationId && s.EndDate > DateTime.Now)
        .Get();

        return response.Models.Any();
    }

    public async Task<bool> UpdateAsync(Guid id, SchedulesDto dto)
    {
        if (id == Guid.Empty) return false;

        var response = await _client.From<Schedule>().Where(s => s.Id == id).Get();
        var current = response.Models.FirstOrDefault();
        if (current == null) return false;

        var finalLocationId = dto.Location?.Id ?? current.LocationId;
        var finalGroupId = dto.Group?.Id ?? current.GroupId;
        var finalStart = dto.StartDate ?? current.StartDate;
        var finalEnd = dto.EndDate ?? current.EndDate;

        bool changed = (dto.StartDate != null && dto.StartDate != current.StartDate) || 
                    (dto.EndDate != null && dto.EndDate != current.EndDate) ||
                    (dto.Location?.Id != null && dto.Location.Id != current.LocationId) ||
                    (dto.Group?.Id != null && dto.Group.Id != current.GroupId);

        if (changed)
        {
            if (await IsLocationOccupiedAsync(finalLocationId, finalStart, finalEnd, id))
                throw new InvalidOperationException($"LOCATION_OCCUPIED|{finalStart:dd/MM HH:mm}");

            if (await IsGroupOccupiedAsync(finalGroupId, finalStart, finalEnd, id))
                throw new InvalidOperationException($"GROUP_OCCUPIED|{finalStart:dd/MM HH:mm}");

            if (!await IsTeacherAvailable(finalGroupId, finalStart, finalEnd, id))
                throw new InvalidOperationException($"TEACHER_OCCUPIED|{finalStart:dd/MM HH:mm}");

            if (await HasConflicWithSubjectsFromTheSameCourse(finalGroupId, finalStart, finalEnd, id))
                throw new InvalidOperationException($"SUBJECT_CONFLICT|{finalStart:dd/MM HH:mm}");
        }

        current.GroupId = finalGroupId;
        current.StartDate = finalStart;
        current.EndDate = finalEnd;
        current.LocationId = finalLocationId;

        await _client.From<Schedule>().Update(current);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _client.From<Schedule>().Where(s => s.Id == id).Delete();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsLocationOccupiedAsync(Guid locationId, DateTime start, DateTime end, Guid? excludeId = null)
    {
        var response = await _client
            .From<Schedule>()
            .Where(s => s.LocationId == locationId)
            .Where(s => s.StartDate < end)
            .Where(s => s.EndDate > start)
            .Get();

        var conflicts = response.Models;

        // Si estamos editando, ignoramos el registro que estamos modificando
        if (excludeId.HasValue)
        {
            conflicts = conflicts.Where(c => c.Id != excludeId.Value).ToList();
        }

        return conflicts.Any();
    }

    public async Task<bool> IsGroupOccupiedAsync(Guid groupId, DateTime start, DateTime end, Guid? excludeId = null)
    {
        var response = await _client
            .From<Schedule>()
            .Where(s => s.GroupId == groupId)
            .Where(s => s.StartDate < end)
            .Where(s => s.EndDate > start)
            .Get();

        var conflicts = response.Models;

        // Si estamos editando un registro existente, lo excluimos de la comparación
        if (excludeId.HasValue)
        {
            conflicts = conflicts.Where(c => c.Id != excludeId.Value).ToList();
        }

        return conflicts.Any();
    }

    public async Task<bool> IsTeacherAvailable(Guid groupId, DateTime start, DateTime end, Guid? excludeScheduleId = null)
    {
        var response = await _groupsService.GetById(groupId);
        if(response == null) return false;
        var teacherGroups = await _groupsService.GetTeacherGroupsbyTeacherId(response.TeacherId);
        foreach (var group in teacherGroups)
        {
            var schedules = await GetSchedulesByGroupIdAsync(group.Id ?? Guid.Empty);

            var hasConflict = schedules.Any(s => 
                s.Id != excludeScheduleId && 
                s.StartDate < end && 
                s.EndDate > start);

            if (hasConflict)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> HasConflicWithSubjectsFromTheSameCourse(Guid groupId, DateTime start, DateTime end, Guid? excludeScheduleId = null)
    {
        var response = await _groupsService.GetById(groupId);
        if(response == null) return false;
        var subjectGroups = await _groupsService.GetSameCourseGroups(response.SubjectId);
        foreach (var group in subjectGroups)
        {
            var schedules = await GetSchedulesByGroupIdAsync(group.Id ?? Guid.Empty);

            var hasConflict = schedules.Any(s => 
                s.Id != excludeScheduleId && 
                s.StartDate < end && 
                s.EndDate > start);

            if (hasConflict)
            {
                return true;
            }
        }

        return false;
    }
}