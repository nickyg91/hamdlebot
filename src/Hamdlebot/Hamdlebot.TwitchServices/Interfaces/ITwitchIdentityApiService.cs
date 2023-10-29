using Hamdlebot.Models;

namespace Hamdlebot.TwitchServices.Interfaces;

public interface ITwitchIdentityApiService
{
    Task<ClientCredentialsTokenResponse?> GetToken(ClientCredentialsTokenRequest request);
}