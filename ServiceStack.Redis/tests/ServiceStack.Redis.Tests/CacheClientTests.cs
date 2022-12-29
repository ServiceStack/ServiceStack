using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
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
    }
}