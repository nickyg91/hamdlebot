using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Models.Twitch;

public class EventSubRequest
{
    [JsonPropertyName("type"), JsonConverter(typeof(SubscriptionTypeEnumConverter))]
    public SubscriptionType Type { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
    [JsonPropertyName("condition")]
    public Dictionary<string, string> Condition { get; set; }
    [JsonPropertyName("transport")]
    public EventSubTransportRequest Transport { get; set; }
}