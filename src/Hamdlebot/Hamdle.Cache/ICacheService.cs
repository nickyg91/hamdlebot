using StackExchange.Redis;

namespace Hamdle.Cache;

public interface ICacheService
{
    ConnectionMultiplexer Connect(byte numberOfRetries);
    IDatabase Database { get; }
    ISubscriber Subscriber { get; }
    Task AddToSet(string key, string item);
    Task<List<string>> GetItemsInSet(string key);
    Task RemoveFromSet(string key, string item);
    Task<int> GetTotalFromSet(string key);
    Task<string?> GetRandomItemFromSet(string key);
    Task<bool> ContainsMember(string key, string item);
    Task<bool> KeyExists(string key);
    Task AddItem(string key, string item, TimeSpan? expiry = null);
    Task<string?> GetItem(string key);
}