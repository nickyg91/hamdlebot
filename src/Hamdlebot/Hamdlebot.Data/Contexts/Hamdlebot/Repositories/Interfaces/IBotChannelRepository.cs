using Hamdlebot.Data.Contexts.Hamdlebot.Entities;

namespace Hamdlebot.Data.Contexts.Hamdlebot.Repositories.Interfaces;

public interface IBotChannelRepository
{
    Task<BotChannel?> GetBotChannelAsync(int twitchUserId);
    Task<BotChannel?> DeleteBotChannelAsync(int id);
    Task CreateBotChannelAsync(BotChannel botChannel);
}