using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Models.ViewModels;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchChatService
{
    Task JoinBotToChannel(Channel channel);
    Task LeaveChannel(long twitchUserId);
    Task JoinExistingChannels();
    Task ConnectToObs(long twitchUserId);
    Task DisconnectFromObs(long twitchUserId);
    void SetCancellationToken(CancellationToken token);
    void UpdateChannelSettings(Channel channel);
}