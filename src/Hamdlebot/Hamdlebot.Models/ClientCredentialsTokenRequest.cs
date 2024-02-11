namespace Hamdlebot.Models;

public class ClientCredentialsTokenRequest
{
    public string GrantType { get; set; }
    public string RefreshToken { get; set; }
}