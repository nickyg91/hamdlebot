using System.Text.Json;
using System.Threading.Channels;
using StackExchange.Redis;

namespace Hamdle.Cache.Channels;

public class ChannelSubscription<T> : ISubscription<T> where T : class
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
    public ChannelSubscription(ICacheService cacheService, RedisChannel redisChannel)
    {
        var subscriber = cacheService.Subscriber;
        subscriber.Subscribe(redisChannel, (_, value) =>
        {
            if (value.IsNullOrEmpty)
            {
                return;
            }

            try
            {
                var message = JsonSerializer.Deserialize<T>(value!);
                if (message != null)
                {
                    _channel.Writer.TryWrite(message);
                }
            }
            catch (Exception)
            {
                _channel.Writer.TryWrite((value as T)!);
            }
        });
    }

    public IAsyncEnumerable<T> Subscribe(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}