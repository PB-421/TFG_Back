using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest;

public class SupabaseService<T> : ISupabaseService<T>
    where T : BaseModel, new()
{
    private readonly Supabase.Client _client;

    public SupabaseService(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var response = await _client.From<T>().Get();
        return response.Models;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var model = await _client
            .From<T>()
            .Filter("id", Constants.Operator.Equals, id)
            .Single();

        return model;
    }

    public async Task<T> CreateAsync(T entity)
    {
        var response = await _client.From<T>().Insert(entity);
        return response.Models.First();
    }

    public async Task UpdateAsync(T entity)
    {
        await _client.From<T>().Update(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _client.From<T>()
            .Filter("id", Constants.Operator.Equals, id)
            .Delete();
    }
}
