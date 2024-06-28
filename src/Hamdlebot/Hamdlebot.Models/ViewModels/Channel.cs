namespace Hamdlebot.Models.ViewModels;

public class Channel
{
    public int Id { get; set; }
    public long TwitchUserId { get; set; }
    public string TwitchChannelName { get; set; }
    public bool IsHamdleEnabled { get; set; }
    public bool AllowAccessToObs { get; set; }
    public List<ChannelCommand> Commands { get; set; } 
}