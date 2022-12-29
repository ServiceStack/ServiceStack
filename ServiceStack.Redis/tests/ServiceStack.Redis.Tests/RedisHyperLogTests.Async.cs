using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Ignore("Integration"), Category("Async")]
    public class RedisHyperLogTestsAsync
    {
        const string Host = "localhost"; // "10.0.0.14"
        private IRedisClientAsync Connect() => new RedisClient(Host);
        
        [Test]
        public async Task Can_Add_to_Hyperlog()
        {
            await using var redis = Connect();

            await redis.FlushAllAsync();

            await redis.AddToHyperLogAsync("hyperlog", new[] { "a", "b", "c" });
            await redis.AddToHyperLogAsync("hyperlog", new[] { "c", "d" });

            var count = await redis.CountHyperLogAsync("hyperlog");

            Assert.That(count, Is.EqualTo(4));

            await redis.AddToHyperLogAsync("hyperlog2", new[] { "c", "d", "e", "f" });

            await redis.MergeHyperLogsAsync("hypermerge", new[] { "hyperlog", "hyperlog2" });

            var mergeCount = await redis.CountHyperLogAsync("hypermerge");

            Assert.That(mergeCount, Is.EqualTo(6));
        }
    }
}