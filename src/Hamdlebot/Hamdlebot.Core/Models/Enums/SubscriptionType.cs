namespace Hamdlebot.Core.Models.Enums;

public enum SubscriptionType
{
    StreamOnline = 1,
    StreamOffline,
    ChannelPollBegin,
    ChannelPollEnd,
    ChannelPollProgress,
    ChannelRaid,
    ChannelFollow,
    ChannelChatMessage,
    ChannelSubscribe,
    ChannelSubscriptionEnd,
    ChannelCheer,
    ChannelBan,
    ChannelUpdate,
    ChannelPredictionBegin,
    ChannelPredictionLocked,
    ChannelPredictionEnd,
    ChannelVipAdd,
    ChannelVipRemove,
    NotSupported,
}