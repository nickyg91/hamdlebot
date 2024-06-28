namespace Hamdlebot.Models.ViewModels;

public class ChannelCommand
{
    public int? Id { get; set; }
    public int BotChannelId { get; set; }
    public string Command { get; set; }
    public string Response { get; set; }
}