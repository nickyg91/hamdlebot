using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Models.Twitch;

public class EventSubRequest
{
    [JsonPropertyName("type"), JsonConverter(typeof(SubscriptionTypeEnumConverter))]
    public SubscriptionType Type { get; set; }
    [JsonPropertyName("version")]
    public byte Version { get; set; }
    [JsonPropertyName("condition")]
    public Dictionary<string, string> Condition { get; set; }
    [JsonPropertyName("transport")]
    public EventSubTransportRequest Transport { get; set; }
}