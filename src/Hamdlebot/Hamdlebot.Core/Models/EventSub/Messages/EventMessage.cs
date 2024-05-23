using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub.Messages;

public class EventMessage
{
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; }
    [JsonPropertyName("payload")]
    public Payload? Payload { get; set; }
}