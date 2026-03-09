using Supabase;
using Supabase.Gotrue;
using System.Text;
using System.Text.Json;

public class ProfilesAppService : IProfilesAppService
{
    private readonly Supabase.Client _client;

    public ProfilesAppService(Supabase.Client client)
    {
        _client = client;
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

            Console.WriteLine(userId);

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

    // 👑 Obtener todos los perfiles (solo admin)
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
            .Get();

            return result.Models;
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<Profile>> GetAllProfilesInternaly()
    {
        var result = await _client
            .From<Profile>()
            .Select("*")
            .Get();

        return result.Models;
    }

    // ---------------- CHANGE ROLE ----------------
    public async Task<bool> UpdateUserAsync(Guid userId, string? newRole, string? newName, string refreshToken)
    {
        try
        {
            var currentUser = await GetCurrentUserProfileAsync(refreshToken);

            if (currentUser == null || currentUser.Role != "admin")
                return false;

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