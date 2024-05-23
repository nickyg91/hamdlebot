using Hamdlebot.Models.Twitch;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchApiService
{
    Task<TwitchApiResponse<GetUsersResponse>?> GetUsersByLogin(List<string> users);
    Task<TwitchApiResponse<EventSubResponse>?> SubscribeToEvents(EventSubRequest request);
}