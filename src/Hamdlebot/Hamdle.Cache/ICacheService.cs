using StackExchange.Redis;

namespace Hamdle.Cache;

public interface ICacheService
{
    ConnectionMultiplexer Connect(byte numberOfRetries);
    IDatabase Database { get; }
    Task AddToSet(string key, string item);
    Task<List<string>> GetItemsInSet(string key);
    Task RemoveFromSet(string key, string item);
    Task<int> GetTotalFromSet(string key);
    Task<string?> GetRandomItemFromSet(string key);
    Task<bool> ContainsMember(string key, string item);
    Task<bool> KeyExists(string key);
}