using Supabase.Gotrue;

public interface IAuthAppService
{
    Task<(Session?, string?)> RegisterAsync(LoginDto request);

    Task<(Session?, Profile?, string?)> LoginAsync(LoginDto request);

    Task<(string accessToken, string refreshToken, int expiresIn, Profile?)?> 
        RefreshSessionAsync(string refreshToken);

    Task<Profile?> GetOrCreateOAuthProfile(LoginDto data);

    Task LogoutAsync();
}