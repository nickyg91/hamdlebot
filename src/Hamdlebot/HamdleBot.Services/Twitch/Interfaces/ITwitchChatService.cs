using Hamdlebot.Models.ViewModels;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchChatService
{
    Task JoinBotToChannel(Channel channel);
    Task LeaveChannel(long twitchUserId);
    Task JoinExistingChannels();
    void SetCancellationToken(CancellationToken token);
}