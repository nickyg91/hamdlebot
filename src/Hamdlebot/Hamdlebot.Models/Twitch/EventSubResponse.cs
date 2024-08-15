using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Models.Twitch;

public record EventSubResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; }
    [JsonPropertyName("status")]
    public string Status { get; init; }
    [JsonPropertyName("type"), JsonConverter(typeof(SubscriptionTypeEnumConverter))]
    public SubscriptionType Type { get; init; }
    [JsonPropertyName("version")]
    public string Version { get; init; }
    [JsonPropertyName("condition")]
    public Dictionary<string, string> Condition { get; init; }
    [JsonPropertyName("created_at")]
    public DateTime CreatedAtUtc { get; init; }
}