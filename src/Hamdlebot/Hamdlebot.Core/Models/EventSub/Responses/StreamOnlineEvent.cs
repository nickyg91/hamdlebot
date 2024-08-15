using System.Text.Json.Serialization;
using Hamdlebot.Core.Converters;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Core.Models.EventSub.Responses;

public class StreamOnlineEvent
{
    /// <summary>
    /// The id of the stream.
    /// </summary>
    [JsonPropertyName("id")]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    ///	The broadcaster’s user ID.
    /// </summary>
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = string.Empty;

    /// <summary>
    /// The broadcaster’s user login. (Lowercase)
    /// </summary>
    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; } = string.Empty;

    /// <summary>
    /// The broadcaster’s user display name.
    /// </summary>
    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUsername { get; set; } = string.Empty;

    /// <summary>
    /// The stream type.
    /// </summary>
    [JsonPropertyName("type"), JsonConverter(typeof(StreamTypeEnumConverter))]
    public StreamType Type { get; set; }

    /// <summary>
    /// The timestamp at which the stream went online at.<br/>
    /// (Converted to DateTimeOffset)
    /// </summary>
    [JsonPropertyName("started_at"), JsonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset? StartedAt { get; set; }

}