using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core.Models.Enums;

namespace Hamdlebot.Core.Converters;

public class SubscriptionTypeEnumConverter : JsonConverter<SubscriptionType>
{
    public override SubscriptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return SubscriptionType.NotSupported;
        }

        return value switch
        {
            "channel.follow" => SubscriptionType.ChannelFollow,
            "stream.online" => SubscriptionType.StreamOnline,
            "stream.offline" => SubscriptionType.StreamOffline,
            "channel.update" => SubscriptionType.ChannelUpdate,
            "channel.raid" => SubscriptionType.ChannelRaid,
            "channel.poll.begin" => SubscriptionType.ChannelPollBegin,
            "channel.poll.progress" => SubscriptionType.ChannelPollProgress,
            "channel.poll.end" => SubscriptionType.ChannelPollEnd,
            "channel.prediction.begin" => SubscriptionType.ChannelPredictionBegin,
            "channel.prediction.locked" => SubscriptionType.ChannelPredictionLocked,
            "channel.prediction.end" => SubscriptionType.ChannelPredictionEnd,
            "channel.cheer" => SubscriptionType.ChannelCheer,
            "channel.chat.message" => SubscriptionType.ChannelChatMessage,
            "channel.subscribe" => SubscriptionType.ChannelSubscribe,
            "channel.subscription.end" => SubscriptionType.ChannelSubscriptionEnd,
            "channel.ban" => SubscriptionType.ChannelBan,
            "channel.vip.add" => SubscriptionType.ChannelVipAdd,
            "channel.vip.remove" => SubscriptionType.ChannelVipRemove,
            _ => SubscriptionType.NotSupported
        };
    }

    public override void Write(Utf8JsonWriter writer, SubscriptionType value, JsonSerializerOptions options)
    {
        var mappedValue = value switch
        {
            SubscriptionType.ChannelFollow => "channel.follow",
            SubscriptionType.StreamOnline => "stream.online",
            SubscriptionType.StreamOffline => "stream.offline",
            SubscriptionType.ChannelUpdate => "channel.update",
            SubscriptionType.ChannelRaid => "channel.raid",
            SubscriptionType.ChannelPollBegin => "channel.poll.begin",
            SubscriptionType.ChannelPollProgress => "channel.poll.progress",
            SubscriptionType.ChannelPollEnd => "channel.poll.end",
            SubscriptionType.ChannelPredictionBegin => "channel.prediction.begin",
            SubscriptionType.ChannelPredictionLocked => "channel.prediction.locked",
            SubscriptionType.ChannelPredictionEnd => "channel.prediction.end",
            SubscriptionType.ChannelCheer => "channel.cheer",
            SubscriptionType.ChannelChatMessage => "channel.chat.message",
            SubscriptionType.ChannelSubscribe => "channel.subscribe",
            SubscriptionType.ChannelSubscriptionEnd => "channel.subscription.end",
            SubscriptionType.ChannelBan => "channel.ban",
            SubscriptionType.ChannelVipAdd => "channel.vip.add",
            SubscriptionType.ChannelVipRemove => "channel.vip.remove",
            SubscriptionType.NotSupported => "",
            // custom exception here at some point
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
        if (writer.CurrentDepth.Equals(1))
        {
            writer.WriteStringValue(mappedValue);    
        }
    }
}