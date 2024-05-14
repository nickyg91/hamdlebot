namespace Hamdlebot.Core.Models.Enums;

public enum SubscriptionType
{
    StreamOnline = 1,
    StreamOffline,
    ChannelPollBegin,
    ChannelPollEnd,
    ChannelPollProgress,
    ChannelRaid,
    ChannelFollow
}