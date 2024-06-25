using Hamdlebot.Data.Contexts.Hamdlebot.Entities;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchChatService : IWebSocketEnabledService
{
    Task JoinBotToChannel(BotChannel channel);
    Task LeaveChannel(long twitchUserId);
    Task JoinExistingChannels();
    Task ConnectToObs(long twitchUserId);
    Task DisconnectFromObs(long twitchUserId);
    void SetCancellationToken(CancellationToken token);
}