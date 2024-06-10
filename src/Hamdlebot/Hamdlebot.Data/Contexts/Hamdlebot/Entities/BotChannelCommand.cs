namespace Hamdlebot.Data.Contexts.Hamdlebot.Entities;

public class BotChannelCommand : BaseEntity
{
    public int BotChannelId { get; set; }
    public string Command { get; set; }
    public string Response { get; set; }
    public BotChannel BotChannel { get; set; }
}