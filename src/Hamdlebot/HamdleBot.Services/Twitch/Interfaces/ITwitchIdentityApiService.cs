using Hamdlebot.Models;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchIdentityApiService
{
    Task<ClientCredentialsTokenResponse?> GetToken(ClientCredentialsTokenRequest request);
}