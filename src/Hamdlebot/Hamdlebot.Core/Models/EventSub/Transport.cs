using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub;

public class Transport
{
    [JsonPropertyName("method")]
    public string Method { get; set; }
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }
}