using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    public class CacheClientTestsAsync
    {
        IRedisClientsManagerAsync redisManager = new RedisManagerPool(TestConfig.SingleHost);
        
        [Test]
        public async Task Can_get_set_CacheClient_Async()
        {
            await using var cache = await redisManager.GetCacheClientAsync();
            await cache.FlushAllAsync();

            await cache.SetAsync("key", "A");
            var result = await cache.GetAsync<string>("key");
            Assert.That(result, Is.EqualTo("A"));
        }
    }
}