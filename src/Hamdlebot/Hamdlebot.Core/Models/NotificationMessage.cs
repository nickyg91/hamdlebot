using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Models;

public class NotificationMessage
{
    public Guid Id { get; set; }
    public StatusType Status { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public string Version { get; set; }
    public int Cost { get; set; }
}