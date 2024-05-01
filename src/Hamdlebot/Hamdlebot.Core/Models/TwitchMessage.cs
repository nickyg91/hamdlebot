namespace Hamdlebot.Core.Models;

public record TwitchMessage
{
    public string? DisplayName { get; set; }
    public bool IsFirstMessage { get; set; }
    public Guid? MessageId { get; set; }
    public bool IsMod { get; set; }
    public bool IsSubscriber { get; set; }
    public int UserId { get; set; }
    public string? User { get; set; }
    public string? Message { get; set; }
}