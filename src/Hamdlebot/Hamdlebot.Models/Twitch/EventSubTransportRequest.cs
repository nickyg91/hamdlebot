using System.Text.Json.Serialization;

namespace Hamdlebot.Models.Twitch;

public record EventSubTransportRequest
{
    [JsonPropertyName("method")]
    public string Method => "websocket";
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }
};