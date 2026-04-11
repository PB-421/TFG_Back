using Supabase;

public class ControlAppService: IControlAppService
{
     private readonly Client _client;

    public ControlAppService(Client client)
    {
        _client = client;
    }

    public async Task<bool> GetStatusByName(string name)
    {
        var response = await _client
        .From<Control>()
        .Where(c => c.Name == name)
        .Get();

        var control = response.Models.FirstOrDefault();
        if (control == null) return false;

        return control.Active;
    }

    public async Task<bool> UpdateStatusByName(string name)
    {
        var response = await _client
        .From<Control>()
        .Where(c => c.Name == name)
        .Get();

        var control = response.Models.FirstOrDefault();
        if (control == null) return false;

        control.Active = !control.Active;

        await _client
            .From<Control>()
            .Where(c => c.Id == control.Id)
            .Update(control);

        return control.Active;
    }
}