using System.Text;
using System.Text.Json;

public class ProfilesAppService : IProfilesAppService
{
    private readonly Supabase.Client _client;

    private readonly ISubjectsAppService _subjectsService;

    public ProfilesAppService(Supabase.Client client, ISubjectsAppService subjectService)
    {
        _client = client;
        _subjectsService = subjectService;
    }

    // 🔐 Obtener usuario actual desde refreshToken
    public async Task<Profile?> GetCurrentUserProfileAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return null;

        try
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Add(
                "apikey",
                Environment.GetEnvironmentVariable("DB_KEY")!
            );

            var body = new
            {
                refresh_token = refreshToken
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await http.PostAsync(
                Environment.GetEnvironmentVariable("DB_URL") +
                "/auth/v1/token?grant_type=refresh_token",
                content
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var session = JsonSerializer.Deserialize<SupabaseRefreshResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (session == null || session.user == null)
                return null;

            var userId = Guid.Parse(session.user.id);

            return await _client
                .From<Profile>()
                .Where(p => p.Id == userId)
                .Single();
        }
        catch
        {
            return null;
        }
    }

    public async Task<Profile?> GetCurrentUserProfileAsync(Guid id)
    {
        try
        {
            return await _client
                .From<Profile>()
                .Where(p => p.Id == id)
                .Single();
        }
        catch
        {
            return null;
        }
    }

    // Obtener todos los perfiles (solo admin)
    public async Task<List<Profile>> GetAllProfilesAsync(string refreshToken)
    {
        try
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Add(
                "apikey",
                Environment.GetEnvironmentVariable("DB_KEY")!
            );

            var body = new
            {
                refresh_token = refreshToken
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await http.PostAsync(
                Environment.GetEnvironmentVariable("DB_URL") +
                "/auth/v1/token?grant_type=refresh_token",
                content
            );

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();

            var session = JsonSerializer.Deserialize<SupabaseRefreshResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (session == null || session.user == null)
                return [];

            var userId = Guid.Parse(session.user.id);

            var profile = await _client
            .From<Profile>()
            .Where(p => p.Id == userId)
            .Single();

            if(profile!.Role != "admin") return [];

            var result = await _client
            .From<Profile>()
            .Select("*")
            .Where(p => p.Id != userId)
            .Get();

            return result.Models;
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<Profile>> GetAllProfilesAsync(Guid id)
    {
        try
        {
            var profile = await _client
            .From<Profile>()
            .Where(p => p.Id == id)
            .Single();
            if(profile!.Role != "admin") return [];

            var result = await _client
            .From<Profile>()
            .Select("*")
            .Where(p => p.Id != profile!.Id)
            .Get();

            return result.Models;
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<profileDto>> GetAllProfilesInternaly()
    {
        var result = await _client
            .From<Profile>()
            .Select("*")
            .Get();
        
        var tasks = result.Models.Select(async p => new profileDto
            {
                Id = p.Id,
                Email = p.Email,
                Name = p.Name,
                Role = p.Role,
                Subjects = await _subjectsService.GetSubjectNamesByIds(p.Subjects.ToList())
            });

            var dtoList = await Task.WhenAll(tasks);

            return dtoList.ToList();

    }

    public async Task<profileDto?> GetProfileById(Guid id)
    {
        var result = await _client
            .From<Profile>()
            .Select("*")
            .Where(p => p.Id == id)
            .Single();
        if(result == null) return null;
        var profile = new profileDto
        {
            Id = result.Id,
            Email = result.Email,
            Name = result.Name,
            Role = result.Role,
            Subjects = await _subjectsService.GetSubjectNamesByIds(result.Subjects.ToList())
        };

        return profile;
    }

    // ---------------- UPDATE----------------
    public async Task<bool> UpdateUserAsync(Guid userId, string? newRole, string? newName)
    {
        try
        {
            var profile = await _client
                .From<Profile>()
                .Where(p => p.Id == userId)
                .Single();

            if (profile == null)
                return false;

            bool hasChanges = false;

            // comprobar role
            if (!string.IsNullOrWhiteSpace(newRole) && profile.Role != newRole)
            {
                profile.Role = newRole;
                hasChanges = true;
            }

            // comprobar name
            if (!string.IsNullOrWhiteSpace(newName) && profile.Name != newName)
            {
                profile.Name = newName;
                hasChanges = true;
            }

            // si no hay cambios, no actualizar
            if (!hasChanges)
                return false;

            await _client
                .From<Profile>()
                .Update(profile);

            return true;
        }
        catch
        {
            return false;
        }
    }
}