using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Models.EventSub;

public class Subscription
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("status"), JsonConverter(typeof(SubscriptionStatusTypeEnumConverter))]
    public SubscriptionStatusType Status { get; set; }
    [JsonPropertyName("type"), JsonConverter(typeof(SubscriptionTypeEnumConverter))]
    public SubscriptionType Type { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
    [JsonPropertyName("cost")]
    public byte Cost { get; set; }
    [JsonPropertyName("condition")]
    public Condition Condition { get; set; }
    [JsonPropertyName("transport")]
    public Transport Transport { get; set; }
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}