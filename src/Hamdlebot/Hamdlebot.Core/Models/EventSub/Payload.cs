using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub;

public class Payload
{
   [JsonPropertyName("session")]
   public Session Session { get; set;}
   [JsonPropertyName("subscription")]
   public Subscription Subscription { get; set;}
   [JsonPropertyName("event")]
   public TwitchEventUserInformation Event { get; set;}
}