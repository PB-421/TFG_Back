public interface IControlAppService
{
    Task<bool> GetStatusByName(string name);
    Task<bool> UpdateStatusByName(string name);
}