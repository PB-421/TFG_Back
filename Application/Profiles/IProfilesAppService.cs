public interface IProfilesAppService
{
    Task<Profile?> GetCurrentUserProfileAsync(string refreshToken);
    Task<List<Profile>> GetAllProfilesAsync(string refreshToken);
    Task<List<Profile>> GetAllProfilesInternaly();
    Task<bool> UpdateUserAsync(Guid userId, string? newRole, string? newName, string refreshToken);
}