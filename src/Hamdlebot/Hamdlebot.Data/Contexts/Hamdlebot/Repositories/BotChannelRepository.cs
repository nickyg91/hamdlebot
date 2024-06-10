using Hamdlebot.Data.Contexts.Hamdlebot.Entities;
using Hamdlebot.Data.Contexts.Hamdlebot.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hamdlebot.Data.Contexts.Hamdlebot.Repositories;

public class BotChannelRepository : IBotChannelRepository
{
    private readonly HamdlebotContext _context;

    public BotChannelRepository(HamdlebotContext context)
    {
        _context = context;
    }
    
    public async Task<BotChannel?> GetBotChannelAsync(int channelId)
    {
        return await _context.BotChannels.FirstOrDefaultAsync(x => x.ChannelId == channelId);
    }

    public async Task<BotChannel?> DeleteBotChannelAsync(int id)
    {
        var botChannel = await _context.BotChannels.FirstOrDefaultAsync(x => x.Id == id);
        if (botChannel == null)
        {
            return null;
        }

        _context.BotChannels.Remove(botChannel);
        return botChannel;
    }

    public async Task CreateBotChannelAsync(BotChannel botChannel)
    {
        await _context.BotChannels.AddAsync(botChannel);
    }
}