using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

public class CacheClientTests
    : RedisClientTestsBase
{
    IRedisClientsManager redisManager = new RedisManagerPool(TestConfig.SingleHost);
        
    [Test]
    public void Can_get_set_CacheClient()
    {
        var cache = redisManager.GetCacheClient();
        cache.FlushAll();

        cache.Set("key", "A");
        var result = cache.Get<string>("key");
        Assert.That(result, Is.EqualTo("A"));
    }

    [Test]
    public void Can_add_bool_value()
    {
        var cache = redisManager.GetCacheClient();
        cache.FlushAll();
        var cacheKey = $"BoolValue";

        cache.Add(cacheKey, false, DateTime.UtcNow.AddMinutes(1));
        Assert.That(cache.Get<bool>(cacheKey), Is.False);

        cache.FlushAll();
        cache.Add(cacheKey, true, DateTime.UtcNow.AddMinutes(1));
        Assert.That(cache.Get<bool>(cacheKey), Is.True);
    }
}