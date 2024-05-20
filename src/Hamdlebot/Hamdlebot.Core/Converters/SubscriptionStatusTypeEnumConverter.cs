using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Converters;

public class SubscriptionStatusTypeEnumConverter : JsonConverter<SubscriptionStatusType>
{
    public override SubscriptionStatusType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return SubscriptionStatusType.Unknown;
        }
        return value switch
        {
            "enabled" => SubscriptionStatusType.Enabled,
            "disabled" => SubscriptionStatusType.Disbabled,
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, SubscriptionStatusType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}