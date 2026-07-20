using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SinalVortex.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SinalVortex.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IDistributedCache distributedCache, IConnectionMultiplexer redis)
    {
        _distributedCache = distributedCache;
        _redis = redis;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(60)
        };

        var json = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(key, json, options);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _distributedCache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task RemoveAsync(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }

    public async Task EnqueueAsync<T>(string queueName, T item)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(item);
        await db.ListLeftPushAsync(queueName, json);
    }

    public async Task<T?> DequeueAsync<T>(string queueName)
    {
        var db = _redis.GetDatabase();
        RedisValue redisValue = await db.ListRightPopAsync(queueName);

        if (redisValue.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(redisValue.ToString()!);
    }
}