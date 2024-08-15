using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub;

public class Payload<T> : PayloadBase where T : class
{
   [JsonPropertyName("session")]
   public Session? Session { get; set; }

   [JsonPropertyName("subscription")]
   public Subscription? Subscription { get; set; }

   [JsonPropertyName("event")]
   public T? Event { get; set; }

}