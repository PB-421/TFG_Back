public interface IProfilesAppService
{
    Task<Profile?> GetCurrentUserProfileAsync(string refreshToken);
    Task<List<Profile>> GetAllProfilesAsync(string refreshToken);
    Task<List<Profile>> GetAllProfilesInternaly();
}