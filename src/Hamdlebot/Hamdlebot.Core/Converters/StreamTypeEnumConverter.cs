using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Core.Converters;

public class StreamTypeEnumConverter : JsonConverter<StreamType>
{
    public override StreamType Read
        (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "all" => StreamType.All,
            "live" => StreamType.Live,
            "playlist" => StreamType.Playlist,
            "watch_party" => StreamType.WatchParty,
            "premiere" => StreamType.Premiere,
            "rerun" => StreamType.Rerun,
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, StreamType value, JsonSerializerOptions options)
    {
        var mappedValue = value switch
        {
            StreamType.All => "all",
            StreamType.Live => "live",
            StreamType.Playlist => "playlist",
            StreamType.WatchParty => "watch_party",
            StreamType.Premiere => "premiere",
            StreamType.Rerun => "rerun",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
        if (writer.CurrentDepth.Equals(1))
        {
            writer.WriteStringValue(mappedValue);
        }
    }
}