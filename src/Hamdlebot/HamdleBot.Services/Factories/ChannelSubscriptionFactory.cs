using Hamdle.Cache;
using Hamdle.Cache.Channels;
using StackExchange.Redis;

namespace HamdleBot.Services.Factories;

public static class ChannelSubscriptionFactory
{
    public static ChannelSubscription<T> CreateSubscription<T>(ICacheService cacheService, RedisChannel channel) where T : class
    {
        return new ChannelSubscription<T>(cacheService, channel);
    }
}