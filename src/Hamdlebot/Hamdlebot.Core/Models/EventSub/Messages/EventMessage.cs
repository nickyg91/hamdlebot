using System.Text.Json.Serialization;

namespace Hamdlebot.Core.Models.EventSub.Messages;

public class EventMessage<T> where T : class
{
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; } = null!;

    [JsonPropertyName("payload")]
    public Payload<T>? Payload { get; set; }
}