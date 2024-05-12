using StackExchange.Redis;

namespace Hamdle.Cache.Channels;

public interface ISubscription<out T>
{
    IAsyncEnumerable<T> Subscribe(CancellationToken cancellationToken);
}