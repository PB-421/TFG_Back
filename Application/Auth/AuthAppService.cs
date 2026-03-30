using Supabase.Gotrue;
using System.Text;
using System.Text.Json;

public class AuthAppService : IAuthAppService
{
    private readonly Supabase.Client _client;

    public AuthAppService(Supabase.Client client)
    {
        _client = client;
    }

    // ---------------- REGISTER ----------------
    public async Task<(Session?, string?)> RegisterAsync(LoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return (null, "Email y password son requeridos");

        var session = await _client.Auth.SignUp(request.Email, request.Password);

        if (session?.User == null)
            return (null, "No se pudo crear el usuario");

        var profile = new Profile
        {
            Id = Guid.Parse(session.User.Id!),
            Email = request.Email,
            Name = request.Name ?? "",
            Role = request.Role ?? "student"
        };

        await _client.From<Profile>().Insert(profile);

        return (session, null);
    }

    // ---------------- LOGIN ----------------
    public async Task<(Session?, Profile?, string?)> LoginAsync(LoginDto request)
    {
        var session = await _client.Auth.SignIn(request.Email, request.Password);

        if (session?.User == null)
            return (null, null, "Credenciales incorrectas");

        var userId = Guid.Parse(session.User.Id!);

        var profile = await _client
            .From<Profile>()
            .Where(p => p.Id == userId)
            .Single();

        return (session, profile, null);
    }

    // ---------------- OAUTH PROFILE ----------------
    public async Task<Profile?> GetOrCreateOAuthProfile(LoginDto data)
    {
        if (string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Id))
            return null;

        var result = await _client
            .From<Profile>()
            .Where(p => p.Email == data.Email)
            .Select("*")
            .Get();

        var profile = result.Models.FirstOrDefault();

        if (profile == null)
        {
            profile = new Profile
            {
                Id = Guid.Parse(data.Id),
                Email = data.Email,
                Name = data.Name!,
                Role = "student"
            };

            await _client.From<Profile>().Insert(profile);
        }

        return profile;
    }

    // ---------------- REFRESH SESSION ----------------
    public async Task<(string accessToken, string refreshToken, int expiresIn, Profile?)?> RefreshSessionAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return null;

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

        var profile = await _client
            .From<Profile>()
            .Where(p => p.Id == userId)
            .Single();

        return (
            session.access_token,
            session.refresh_token,
            session.expires_in,
            profile
        );
    }

    // ---------------- LOGOUT ----------------
    public async Task LogoutAsync()
    {
        await _client.Auth.SignOut();
    }

    // ---------------- DELETE USER ----------------
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Add(
                "apikey",
                Environment.GetEnvironmentVariable("DB_SUDOKEY")!
            );

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    Environment.GetEnvironmentVariable("DB_SUDOKEY")
                );

            var response = await http.DeleteAsync(
                Environment.GetEnvironmentVariable("DB_URL") +
                $"/auth/v1/admin/users/{userId}"
            );

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}