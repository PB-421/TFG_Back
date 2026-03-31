using Supabase;
public class RequestsAppService : IRequestsAppService
{
    private readonly Client _client;

    private readonly IGroupsAppService _groupsRepo;
    private readonly ILocationsAppService _locationService;
    private readonly ISchedulesAppService _schedulesRepo;
    public RequestsAppService(Client client, IGroupsAppService groupsRepo, ISchedulesAppService schedulesRepo, ILocationsAppService locationService)
    {
        _client = client;
        _groupsRepo = groupsRepo;
        _schedulesRepo = schedulesRepo;
        _locationService = locationService;
    }

    public async Task<List<RequestDto>> GetAllAsync()
    {
        var response = await _client.From<Request>().Select("*").Get();

        return response.Models.Select(r => new RequestDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            OriginGroupId = r.OriginGroupId,
            DestinationGroupId = r.DestinationGroupId,
            Weight = r.Weight,
            StudentComment = r.StudentComment,
            TeacherComment = r.TeacherComment,
            Status = r.Status,
            PdfPath = r.PdfPath
        }).ToList();
    }

    public async Task<List<RequestDto>> GetByStudentId(Guid StudentId)
    {
        var response = await _client.From<Request>().Select("*").Where(r => r.StudentId == StudentId).Get();

        return response.Models.Select(r => new RequestDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            OriginGroupId = r.OriginGroupId,
            DestinationGroupId = r.DestinationGroupId,
            Weight = r.Weight,
            StudentComment = r.StudentComment,
            TeacherComment = r.TeacherComment,
            Status = r.Status,
            PdfPath = r.PdfPath
        }).ToList();
    }

    public async Task<RequestDto> GetByIdAsync(Guid id)
    {
        var result = await _client
            .From<Request>()
            .Select("*")
            .Where(r => r.Id == id)
            .Single();

        if (result == null) return new RequestDto();

        return new RequestDto
        {
            Id = result.Id,
            StudentId = result.StudentId,
            OriginGroupId = result.OriginGroupId,
            DestinationGroupId = result.DestinationGroupId,
            Weight = result.Weight,
            StudentComment = result.StudentComment,
            TeacherComment = result.TeacherComment,
            Status = result.Status,
            PdfPath = result.PdfPath
        };
    }

    public async Task<bool> studentHasGroupRequest(Guid studentId, Guid OriginGroupId)
    {
        var existing = await _client
            .From<Request>()
            .Where(r => r.StudentId == studentId)
            .Where(r => r.Status == 0)
            .Where(r => r.OriginGroupId == OriginGroupId)
            .Get();

        if (existing.Models.Any())
            return true;
        
        return false;
    }

    public async Task<bool> CreateAsync(RequestDto request)
    {
        if(await studentHasGroupRequest(request.StudentId, request.OriginGroupId)) return false;

        var newRequest = new Request
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            OriginGroupId = request.OriginGroupId,
            DestinationGroupId = request.DestinationGroupId,
            Weight = request.Weight,
            StudentComment = request.StudentComment,
            TeacherComment = request.TeacherComment,
            Status = request.Status,
            PdfPath = request.PdfPath
        };

        await _client.From<Request>().Insert(newRequest);
        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, RequestDto request)
    {
        var response = await _client
            .From<Request>()
            .Where(r => r.Id == id)
            .Get();

        var current = response.Models.FirstOrDefault();

        if (current == null)
            return false;

        // Lógica de detección de cambios
        bool hasChanges = false;

        if (current.Status != request.Status) { current.Status = request.Status; hasChanges = true; }
        if (current.TeacherComment != request.TeacherComment) { current.TeacherComment = request.TeacherComment; hasChanges = true; }
        if (current.Weight != request.Weight) { current.Weight = request.Weight; hasChanges = true; }
        
        // Puedes agregar más campos según necesites permitir su edición

        if (!hasChanges)
            return false;

        await _client.From<Request>().Update(current);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _client.From<Request>().Where(r => r.Id == id).Delete();
        return true;
    }

    public async Task<(bool ok, string? error)> ResolveWithMinCostFlowAsync()
    {
        // 1. Obtener todas las solicitudes pendientes (Status 0)
        var allRequests = await GetAllAsync();
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

            await UpdateAsync(req.Id, req);
        }

        pendingRequests = pendingRequests
            .Where(r => !swapsToAccept.Contains(r.Id))
            .ToList();

        // 2. Obtener grupos y calcular capacidades libres
        var groups = (await _groupsRepo.GetAllAsync()).ToList();
        var freeCapacity = new Dictionary<Guid, int>();

        foreach (var group in groups)
        {
            var groupLocations = await _schedulesRepo.GetLocationsById(group.Id!.Value);
            var totalCapacity = await _locationService.GetLocationsCapacityByIds(groupLocations);
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
            
            await UpdateAsync(req.Id, req);
        }

        return (true, "Proceso de optimización finalizado.");
    }

    public async Task<(bool ok, string? error)> ApplyAcceptedRequestsAsync()
    {
        // 1. Obtener solicitudes aceptadas (Status 1)
        var allRequests = await GetAllAsync();
        var acceptedRequests = allRequests.Where(r => r.Status == 2).ToList();

        if (!acceptedRequests.Any())
            return (true, "No hay solicitudes aceptadas para aplicar.");

        // Traemos los grupos (vienen con List<profileDto> en Students)
        var groups = await _groupsRepo.GetAllAsync();

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
                    // Agregamos un profileDto básico (solo el ID es crítico para el UpdateAsync que tienes)
                    dest.Students.Add(new profileDto { Id = r.StudentId });
                    modifiedGroupIds.Add(dest.Id!.Value);
                }

                // --- 3. Marcar solicitud como Aplicada (Status 2) ---
                r.Status = 2;
                r.TeacherComment = "Cambio de grupo aplicado exitosamente.";
                await UpdateAsync(r.Id, r);
            }
        }

        // 2. Actualizar solo los grupos que realmente cambiaron
        foreach (var groupId in modifiedGroupIds)
        {
            var groupDto = groups.First(g => g.Id == groupId);
            // IMPORTANTE: Pasamos 'true' en el parámetro algorithm para que UpdateAsync procese los Students
            await _groupsRepo.UpdateAsync(groupId, groupDto, true);
        }

        return (true, "Cambios aplicados en los listados de alumnos.");
    }
}
