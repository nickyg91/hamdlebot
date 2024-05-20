using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Models.EventSub;

public class Metadata
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; }
    [JsonPropertyName("message_type"), JsonConverter(typeof(MessageTypeEnumConverter))]
    public MessageType MessageType { get; }
    [JsonPropertyName("message_timestamp")]
    public DateTime MessageTimestamp { get; }
}