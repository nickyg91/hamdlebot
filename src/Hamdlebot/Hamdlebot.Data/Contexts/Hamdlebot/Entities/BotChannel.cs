namespace Hamdlebot.Data.Contexts.Hamdlebot.Entities;

public class BotChannel : BaseEntity
{
    public string TwitchChannelName { get; set; }
    public int ChannelId { get; set; }
    public bool IsHamdleEnabled { get; set; }
    public List<BotChannelCommand> BotChannelCommands { get; set; }
}