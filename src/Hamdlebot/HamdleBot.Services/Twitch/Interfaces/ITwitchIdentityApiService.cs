using System.Net;
using Hamdlebot.Models;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchIdentityApiService
{
    Task<ClientCredentialsTokenResponse?> GetToken(ClientCredentialsTokenRequest request);
    Task<ClientCredentialsTokenResponse?> GetTokenFromCodeFlow(ClientCredentialsTokenRequest request);
    Task<ClientCredentialsTokenResponse?> RefreshToken(ClientCredentialsTokenRequest request);
    Task<string?> ListenForRedirect(string redirectUrl);
}