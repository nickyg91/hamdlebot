using System.Text.Json;
using Hamdlebot.Core;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Hamdle.Cache;

public class CacheService : ICacheService
{
    private readonly Lazy<ConnectionMultiplexer> _redisConnection;
    private readonly string _connectionString;
    private readonly byte _maxRetries;
    public IDatabase Database => _redisConnection.Value.GetDatabase();
    public ISubscriber Subscriber => _redisConnection.Value.GetSubscriber();

    public CacheService(IOptions<AppConfigSettings> settings)
    {
        _connectionString = settings.Value.RedisSettingsOptions!.ConnectionString!;
        _maxRetries = settings.Value.RedisSettingsOptions.MaxRetries;
        if (_redisConnection?.Value == null)
        {
            _redisConnection = new Lazy<ConnectionMultiplexer>(() => Connect(0));
        }
    }

    public ConnectionMultiplexer Connect(byte numberOfRetries = 0)
    {
        ConnectionMultiplexer muxer;
        try
        {
            muxer = ConnectionMultiplexer.Connect(_connectionString);
        }
        catch (RedisConnectionException e)
        {
            Console.WriteLine(e);
            if (numberOfRetries < _maxRetries)
            {
                return Connect(numberOfRetries++);
            }
            throw;
        }
        return muxer;
    }
    
    public async Task AddToSet(string key, string item)
    {
        await Database.SetAddAsync(key, item);
    }

    public async Task<List<string>> GetItemsInSet(string key)
    {
        var result = await Database.SetMembersAsync(key);
        return result.Select(x => x.ToString()).ToList();
    }

    public async Task RemoveFromSet(string key, string item)
    {
        await Database.SetRemoveAsync(key, item);
    }

    public async Task<int> GetTotalFromSet(string key)
    {
        return (int)await Database.SetLengthAsync(key);
    }

    public async Task<string?> GetRandomItemFromSet(string key)
    {
        return await Database.SetRandomMemberAsync(key);
    }

    public async Task<bool> ContainsMember(string key, string item)
    {
        return await Database.SetContainsAsync(key, item.ToLower());
    }

    public async Task<bool> KeyExists(string key)
    {
        return await Database.KeyExistsAsync(key);
    }

    public async Task AddItem(string key, string item, TimeSpan? expiry = null)
    {
        await Database.StringSetAsync(key, item, expiry);
    }

    public async Task<string?> GetItem(string key)
    {
        return await Database.StringGetAsync(key);
    }
}