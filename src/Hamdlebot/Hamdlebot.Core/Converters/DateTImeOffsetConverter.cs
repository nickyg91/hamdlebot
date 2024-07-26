using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hamdlebot.Core.Converters;


public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read
        (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
            {
                var unixTimestamp = reader.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            }
            case JsonTokenType.String:
            {
                var value = reader.GetString();
                if (value == null)
                {
                    return null;
                }
                return value == "0" ?
                    DateTimeOffset.UtcNow : DateTimeOffset.Parse(value);
            }
            default:
                throw new JsonException("Expected a string or number, but got " + reader.TokenType);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value != null)
        {
            writer.WriteStringValue(value.Value.ToString("o"));
        }
    }
}