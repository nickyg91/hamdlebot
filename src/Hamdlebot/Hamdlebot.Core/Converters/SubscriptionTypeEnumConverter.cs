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
        throw new NotImplementedException();
    }
}