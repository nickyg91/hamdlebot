using Hamdlebot.Models;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchIdentityApiService
{
    Task<ClientCredentialsTokenResponse> GetToken(string code);
    Task<ClientCredentialsTokenResponse> GetTokenFromCodeFlow();
    Task<ClientCredentialsTokenResponse> RefreshToken(string refreshToken);
    Task<string?> ListenForRedirect(string redirectUrl);
    string GetWorkerAuthorizationCodeUrl();
    string GetClientOIDCAuthorizationCodeUrl();
}