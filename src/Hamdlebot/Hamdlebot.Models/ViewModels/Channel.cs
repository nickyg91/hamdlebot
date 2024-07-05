using Hamdlebot.Data.Contexts.Hamdlebot.Entities;

namespace Hamdlebot.Models.ViewModels;

public class Channel
{
    public Channel()
    {
        
    }

    public Channel(BotChannel channel)
    {
        Id = channel.Id;
        TwitchUserId = channel.TwitchUserId;
        TwitchChannelName = channel.TwitchChannelName;
        IsHamdleEnabled = channel.IsHamdleEnabled;
        AllowAccessToObs = channel.AllowAccessToObs;
        Commands = channel.BotChannelCommands.Select(x => new ChannelCommand(x)).ToList();
    }
    
    public int Id { get; set; }
    public long TwitchUserId { get; set; }
    public string TwitchChannelName { get; set; }
    public bool IsHamdleEnabled { get; set; }
    public bool AllowAccessToObs { get; set; }
    public List<ChannelCommand> Commands { get; set; } 
}