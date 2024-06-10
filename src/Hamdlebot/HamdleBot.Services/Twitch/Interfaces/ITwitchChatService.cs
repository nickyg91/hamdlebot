using Hamdlebot.Data.Contexts.Hamdlebot.Entities;

namespace HamdleBot.Services.Twitch.Interfaces;

public interface ITwitchChatService : IWebSocketEnabledService
{
    Task JoinBotToChannel(BotChannel channel);
    Task JoinExistingChannels();
}