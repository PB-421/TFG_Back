public class AlgorithmsAppService : IAlgorithmsAppService
{
    private readonly IProfilesAppService _usersService;
    private readonly IGroupsAppService _groupsService;
    private readonly ILocationsAppService _locationsService;
    private readonly ISchedulesAppService _schedulesService;
    private readonly IRequestsAppService _requestsService;

    public AlgorithmsAppService(IProfilesAppService usersService, IGroupsAppService groupsService, ILocationsAppService locationService, ISchedulesAppService schedulesService, IRequestsAppService requestsService)
    {
        _usersService = usersService;
        _groupsService = groupsService;
        _locationsService = locationService;
        _schedulesService = schedulesService;
        _requestsService = requestsService;
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
        var allStudents = (await _usersService.GetAllProfilesInternaly())
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
            var groupLocations = await _schedulesService.GetLocationsById(group.Id!.Value);
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

    public async Task<(bool ok, string? error)> ResolveWithMinCostFlowAsync()
    {
        // 1. Obtener todas las solicitudes pendientes (Status 0)
        var allRequests = await _requestsService.GetAllAsync();
        var pendingRequests = allRequests.Where(r => r.Status == 0).ToList();

        if (pendingRequests.Count == 0)
            return (true, "No hay solicitudes pendientes para procesar.");

        // 1.5: Detecion de swap entre grupos
        var swapsToAccept = new HashSet<Guid>();
        var usedInSwap = new HashSet<Guid>();

        for (int i = 0; i < pendingRequests.Count; i++)
        {
            var r1 = pendingRequests[i];

            if (usedInSwap.Contains(r1.Id))
                continue;

            for (int j = i + 1; j < pendingRequests.Count; j++)
            {
                var r2 = pendingRequests[j];

                if (usedInSwap.Contains(r2.Id))
                    continue;

                bool isSwap =
                    r1.OriginGroupId == r2.DestinationGroupId &&
                    r1.DestinationGroupId == r2.OriginGroupId;

                if (isSwap)
                {
                    // Marcar ambos como usados
                    usedInSwap.Add(r1.Id);
                    usedInSwap.Add(r2.Id);

                    swapsToAccept.Add(r1.Id);
                    swapsToAccept.Add(r2.Id);

                    break;
                }
            }
        }

        foreach (var req in pendingRequests.Where(r => swapsToAccept.Contains(r.Id)))
        {
            req.Status = 2; // Aceptada
            req.TeacherComment = "Aceptada por intercambio automático.";

            await _requestsService.UpdateAsync(req.Id, req);
        }

        pendingRequests = pendingRequests
            .Where(r => !swapsToAccept.Contains(r.Id))
            .ToList();

        // 2. Obtener grupos y calcular capacidades libres
        var groups = (await _groupsService.GetAllAsync()).ToList();
        var freeCapacity = new Dictionary<Guid, int>();

        foreach (var group in groups)
        {
            var groupLocations = await _schedulesService.GetLocationsById(group.Id!.Value);
            var totalCapacity = await _locationsService.GetLocationsCapacityByIds(groupLocations);
            var used = group.Students?.Count ?? 0; // Usar Length para arrays
            freeCapacity[group.Id.Value] = Math.Max(0, totalCapacity - used);
        }

        // 3. Configuración del Grafo
        int source = 0;
        int reqStart = 1;
        int grpStart = reqStart + pendingRequests.Count;
        int sink = grpStart + groups.Count;

        var mcmf = new MinCostMaxFlow(sink + 1);

        // Mapeo para identificar qué arista corresponde a qué solicitud
        // Diccionario: <IndiceSolicitud, List<(IndiceGrupo, AristaEnGrafo)>>
        var requestEdges = new Dictionary<int, List<(int groupIdx, int edgeIdx)>>();

        for (int i = 0; i < pendingRequests.Count; i++)
        {
            var req = pendingRequests[i];
            // Source -> Solicitud
            mcmf.AddEdge(source, reqStart + i, 1, 0);

            int gIndex = groups.FindIndex(g => g.Id == req.DestinationGroupId);
            if (gIndex >= 0)
            {
                var group = groups[gIndex];

                // Número actual de alumnos en el grupo
                int currentStudents = group.Students?.Count ?? 0;

                // Penalización por ocupación
                int balancePenalty = currentStudents * 2;

                // Nuevo coste balanceado
                int cost = -req.Weight + balancePenalty;

                int edgeIdx = mcmf.GetGraph()[reqStart + i].Count; 
                mcmf.AddEdge(reqStart + i, grpStart + gIndex, 1, cost);

                requestEdges[i] = new List<(int, int)> { (gIndex, edgeIdx) };
            }
        }

        // Grupos -> Sink
        for (int i = 0; i < groups.Count; i++)
        {
            mcmf.AddEdge(grpStart + i, sink, freeCapacity[groups[i].Id!.Value], 0);
        }

        // 4. Ejecutar Algoritmo
        mcmf.GetMinCostMaxFlow(source, sink);

        // 5. Analizar resultados y actualizar en Supabase
        var graph = mcmf.GetGraph();
        foreach (var entry in requestEdges)
        {
            int reqIdx = entry.Key;
            var req = pendingRequests[reqIdx];
            bool accepted = false;

            foreach (var edgeInfo in entry.Value)
            {
                // Si la capacidad es 0, significa que el flujo de 1 unidad pasó por aquí
                var edge = graph[reqStart + reqIdx][edgeInfo.edgeIdx];
                if (edge.Capacity == 0) 
                {
                    accepted = true;
                    break;
                }
            }

            // Actualizamos el estado: 2 = Aceptada, 0 = sigue pendiente
            req.Status = accepted ? 2 : 0;
            req.TeacherComment = accepted ? "Aceptada por optimización de cupos." : "Sin cupo disponible en esta iteración.";
            
            await _requestsService.UpdateAsync(req.Id, req);
        }

        return (true, "Proceso de optimización finalizado.");
    }

    public async Task<(bool ok, string? error)> ApplyAcceptedRequestsAsync()
    {
        // 1. Obtener solicitudes aceptadas (Status 2)
        var allRequests = await _requestsService.GetAllAsync();
        var acceptedRequests = allRequests.Where(r => r.Status == 2).ToList();

        if (!acceptedRequests.Any())
            return (true, "No hay solicitudes aceptadas para aplicar.");

        // Traemos los grupos (vienen con List<profileDto> en Students)
        var groups = await _groupsService.GetAllAsync();

        // Seguimiento de qué grupos han sido modificados para no actualizar de más
        var modifiedGroupIds = new HashSet<Guid>();

        foreach (var r in acceptedRequests)
        {
            var origin = groups.FirstOrDefault(g => g.Id == r.OriginGroupId);
            var dest = groups.FirstOrDefault(g => g.Id == r.DestinationGroupId);

            if (origin != null && dest != null)
            {
                // --- 1. Remover del origen ---
                if (origin.Students != null && origin.Students.Any(s => s.Id == r.StudentId))
                {
                    origin.Students = origin.Students.Where(s => s.Id != r.StudentId).ToList();
                    modifiedGroupIds.Add(origin.Id!.Value);
                }

                // --- 2. Agregar al destino ---
                if (dest.Students != null && !dest.Students.Any(s => s.Id == r.StudentId))
                {
                    dest.Students.Add(new profileDto { Id = r.StudentId });
                    modifiedGroupIds.Add(dest.Id!.Value);
                }

                // --- 3. Marcar solicitud como Aplicada (Status 2) ---
                r.Status = 2;
                r.TeacherComment = "Cambio de grupo aplicado exitosamente.";
                await _requestsService.UpdateAsync(r.Id, r);
            }
        }

        foreach (var groupId in modifiedGroupIds)
        {
            var groupDto = groups.First(g => g.Id == groupId);
            await _groupsService.UpdateAsync(groupId, groupDto);
        }

        return (true, "Cambios aplicados en los listados de alumnos.");
    }

}