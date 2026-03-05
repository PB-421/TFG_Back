using Supabase;

public class ProfilesAppService : IProfilesAppService
{
    private readonly Client _client;

    public ProfilesAppService(Client client)
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
            await _client.Auth.SetSession(string.Empty, refreshToken, true);
            var session = await _client.Auth.RefreshSession();

            if (session?.User == null)
                return null;

            if (!Guid.TryParse(session.User.Id, out var userId))
                return null;

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
        var currentUser = await GetCurrentUserProfileAsync(refreshToken);

        if (currentUser == null)
            throw new UnauthorizedAccessException("No autenticado");

        if (currentUser.Role != "admin")
            throw new UnauthorizedAccessException("No autorizado");

        var result = await _client
            .From<Profile>()
            .Select("*")
            .Get();

        return result.Models;
    }

    public async Task<List<Profile>> GetAllProfilesInternaly()
    {
        var result = await _client
            .From<Profile>()
            .Select("*")
            .Get();

        return result.Models;
    }
}