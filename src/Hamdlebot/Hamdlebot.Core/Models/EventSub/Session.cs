using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Models.EventSub;

public class Session
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("status"), JsonConverter(typeof(SessionStatusTypeEnumConverter))]
    public SessionStatusType SessionStatus { get; set; }
    [JsonPropertyName("keepalive_timeout_seconds")]
    public int? KeepaliveTimeoutSeconds { get; set; }
    [JsonPropertyName("reconnect_url")]
    public string ReconnectUrl { get; set; }
    [JsonPropertyName("connected_at")]
    public DateTime ConnectedAt { get; set; }
}