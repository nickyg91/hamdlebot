namespace Hamdlebot.Models;

public class ClientCredentialsTokenRequest
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string GrantType { get; set; }
}