namespace Hamdlebot.Data.Contexts.Hamdlebot.Entities;

public class BotChannel : BaseEntity
{
    public long TwitchUserId { get; set; }
    public string TwitchChannelName { get; set; }
    public bool IsHamdleEnabled { get; set; }
    public List<BotChannelCommand> BotChannelCommands { get; set; }
}