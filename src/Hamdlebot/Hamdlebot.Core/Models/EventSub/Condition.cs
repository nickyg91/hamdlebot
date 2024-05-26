using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub;

public class Condition
{
    [JsonPropertyName("broadcaster_user_id")]
    public long BroadcasterUserId { get; set; }
}