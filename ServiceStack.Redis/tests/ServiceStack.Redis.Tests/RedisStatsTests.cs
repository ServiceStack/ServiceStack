using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisStatsTests
        : RedisClientTestsBase
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RedisConfig.AssumeServerVersion = 2821;
        }

        [Test]
        [Ignore("too long")]
        public void Batch_and_Pipeline_requests_only_counts_as_1_request()
        {
            var reqCount = RedisNativeClient.RequestsPerHour;

            var map = new Dictionary<string, string>();
            10.Times(i => map["key" + i] = "value" + i);

            Redis.SetValues(map);

            Assert.That(RedisNativeClient.RequestsPerHour, Is.EqualTo(reqCount + 1));

            var keyTypes = new Dictionary<string, string>();
            using (var pipeline = Redis.CreatePipeline())
            {
                map.Keys.Each(key =>
                    pipeline.QueueCommand(r => r.Type(key), x => keyTypes[key] = x));

                pipeline.Flush();
            }

            Assert.That(RedisNativeClient.RequestsPerHour, Is.EqualTo(reqCount + 2));
            Assert.That(keyTypes.Count, Is.EqualTo(map.Count));
        }
    }
}