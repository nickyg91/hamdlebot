using Hamdlebot.Models;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchIdentityApiService
{
    Task<ClientCredentialsTokenResponse> GetToken(string code);
    Task<ClientCredentialsTokenResponse> GetTokenForBot(string code);
    Task<ClientCredentialsTokenResponse> RefreshToken(string refreshToken);
    string GetWorkerAuthorizationCodeUrl();
    string GetClientOidcAuthorizationCodeUrl();
}