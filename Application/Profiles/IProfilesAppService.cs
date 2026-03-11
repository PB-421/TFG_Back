public interface IProfilesAppService
{
    Task<Profile?> GetCurrentUserProfileAsync(string refreshToken);
    Task<Profile?> GetCurrentUserProfileAsync(Guid id);
    Task<List<Profile>> GetAllProfilesAsync(string refreshToken);
    Task<List<Profile>> GetAllProfilesAsync(Guid id);
    Task<List<profileDto>> GetAllProfilesInternaly();
    Task<profileDto?> GetProfileById(Guid id); 
    Task<bool> UpdateUserAsync(Guid userId, string? newRole, string? newName);
}