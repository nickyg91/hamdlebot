using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Core.Converters;

public class MessageTypeEnumConverter : JsonConverter<MessageType>
{
    public override MessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "session_welcome" => MessageType.SessionWelcome,
            "session_keepalive" => MessageType.SessionKeepalive,
            "notification" => MessageType.Notification,
            "session_reconnect" => MessageType.SessionReconnect,
            "revocation" => MessageType.Revocation,
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}