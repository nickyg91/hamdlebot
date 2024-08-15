namespace Hamdlebot.Core;

public class TwitchConnectionInfo
{
    public string? ClientSecret { get; set; }
    public string? ClientId { get; set; }
    public string? ClientRedirectUrl { get; set; }
    public string? WorkerRedirectUrl { get; set; }
    public string HamdlebotUserId => "978363539";
}