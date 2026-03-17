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

    public async Task<bool> CreateAsync(SchedulesDto dto)
    {
        try
        {
            // Validación de solapamiento antes de crear
            if (dto.StartDate.HasValue && dto.EndDate.HasValue && dto.Location!.Id != null)
            {
                var isOccupied = await IsLocationOccupiedAsync(dto.Location.Id.Value, dto.StartDate.Value, dto.EndDate.Value);
                if (isOccupied) return false; 
            }

            var newSchedule = new Schedule
            {
                Id = dto.Id ?? Guid.NewGuid(),
                GroupId = dto.Group!.Id ?? Guid.Empty,
                StartDate = dto.StartDate ?? DateTime.Now,
                EndDate = dto.EndDate ?? DateTime.Now.AddHours(1),
                LocationId = dto.Location!.Id ?? Guid.Empty
            };

            await _client.From<Schedule>().Insert(newSchedule);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Guid>> GetLocationsById(Guid groupId)
    {
        var locations = (await GetAllAsync())
            .Where(g => g.Group!.Id == groupId)
            .Select(g => g.Location!.Id!.Value)
            .Distinct()
            .ToList();

        return locations;
    }

    public async Task<bool> UpdateAsync(Guid id, SchedulesDto dto)
    {
        if (id == Guid.Empty) return false;

        try
        {
            var response = await _client.From<Schedule>().Where(s => s.Id == id).Get();
            var current = response.Models.FirstOrDefault();
            if (current == null) return false;

            // Si cambian fechas o ubicación, validar disponibilidad
            if ((dto.StartDate != null && dto.StartDate != current.StartDate) || 
                (dto.EndDate != null && dto.EndDate != current.EndDate) ||
                (dto.Location!.Id != null && dto.Location.Id != current.LocationId))
            {
                var isOccupied = await IsLocationOccupiedAsync(
                    dto.Location!.Id ?? current.LocationId, 
                    dto.StartDate ?? current.StartDate, 
                    dto.EndDate ?? current.EndDate,
                    id // Excluimos el registro actual de la validación
                );
                if (isOccupied) return false;
            }

            current.GroupId = dto.Group!.Id ?? current.GroupId;
            current.StartDate = dto.StartDate ?? current.StartDate;
            current.EndDate = dto.EndDate ?? current.EndDate;
            current.LocationId = dto.Location.Id ?? current.LocationId;

            await _client.From<Schedule>().Update(current);
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
        // Lógica de solapamiento: (Inicio1 < Fin2) Y (Fin1 > Inicio2)
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
}