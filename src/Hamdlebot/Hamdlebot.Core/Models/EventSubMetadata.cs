using System.Text.Json.Serialization;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Models;

public class EventSubMetadata
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
    [JsonPropertyName("message_type"), JsonConverter()]
    public MessageType MessageType { get; set; }
    [JsonPropertyName("message_timestamp")]
    public DateTime MessageTimestamp { get; set; }
}