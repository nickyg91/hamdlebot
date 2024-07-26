using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Core.Models.EventSub;

public class Metadata
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set;}
    [JsonPropertyName("message_type"), JsonConverter(typeof(MessageTypeEnumConverter))]
    public MessageType MessageType { get; set;}
    [JsonPropertyName("message_timestamp")]
    public DateTime MessageTimestamp { get; set;}
}