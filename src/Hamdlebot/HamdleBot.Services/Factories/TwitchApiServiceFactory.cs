using HamdleBot.Services.Twitch;
using HamdleBot.Services.Twitch.Interfaces;

namespace HamdleBot.Services.Factories;

public static class TwitchApiServiceFactory
{
    public static ITwitchApiService CreateTwitchApiService(string authToken, string clientId, CancellationToken cancellationToken)
    {
        return new TwitchApiService(authToken, clientId, cancellationToken);
    }
}