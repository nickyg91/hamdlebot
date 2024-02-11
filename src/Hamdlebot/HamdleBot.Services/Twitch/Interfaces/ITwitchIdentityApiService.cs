using System.Net;
using Hamdlebot.Models;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchIdentityApiService
{
    Task<ClientCredentialsTokenResponse?> GetToken();
    Task<ClientCredentialsTokenResponse?> GetTokenFromCodeFlow();
    Task<ClientCredentialsTokenResponse?> RefreshToken(string refreshToken);
    Task<string?> ListenForRedirect(string redirectUrl);
}