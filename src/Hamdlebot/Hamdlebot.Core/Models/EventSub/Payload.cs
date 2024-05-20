using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub;

public class Payload
{
   [JsonPropertyName("event")]
   public TwitchEventUserInformation TwitchEventUserInformation { get; }
   [JsonPropertyName("session")]
   public Session Session { get; }
   [JsonPropertyName("subscription")]
   public Subscription Subscription { get; }
   [JsonPropertyName("event")]
   public TwitchEventUserInformation Event { get; }
}