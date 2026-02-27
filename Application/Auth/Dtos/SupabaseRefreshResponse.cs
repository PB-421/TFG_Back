public class SupabaseRefreshResponse
{
    public string access_token { get; set; } = string.Empty;
    public string refresh_token { get; set; } = string.Empty;
    public int expires_in { get; set; }
    public string token_type { get; set; } = string.Empty;
    public SupabaseUser user { get; set; } = new SupabaseUser();
}

public class SupabaseUser
{
    public string id { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
}