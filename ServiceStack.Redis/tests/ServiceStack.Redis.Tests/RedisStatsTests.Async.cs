using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisStatsTestsAsync
        : RedisClientTestsBaseAsync
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RedisConfig.AssumeServerVersion = 2821;
        }

        [Test]
        [Ignore("too long")]
        public async Task Batch_and_Pipeline_requests_only_counts_as_1_request()
        {
            var reqCount = RedisNativeClient.RequestsPerHour;

            var map = new Dictionary<string, string>();
            10.Times(i => map["key" + i] = "value" + i);

            await RedisAsync.SetValuesAsync(map);

            Assert.That(RedisNativeClient.RequestsPerHour, Is.EqualTo(reqCount + 1));

            var keyTypes = new Dictionary<string, string>();
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                map.Keys.Each(key =>
                    pipeline.QueueCommand(r => r.TypeAsync(key), x => keyTypes[key] = x));

                await pipeline.FlushAsync();
            }

            Assert.That(RedisNativeClient.RequestsPerHour, Is.EqualTo(reqCount + 2));
            Assert.That(keyTypes.Count, Is.EqualTo(map.Count));
        }
    }
}