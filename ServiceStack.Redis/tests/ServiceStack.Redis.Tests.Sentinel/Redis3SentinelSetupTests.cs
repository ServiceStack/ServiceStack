using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Sentinel
{
    [TestFixture, Category("Integration")]
    [Ignore("Requires cloud setup")]
    public class Redis3SentinelSetupTests
        : RedisSentinelTestBase
    {
        [Test]
        public void Can_connect_to_3SentinelSetup()
        {
            var sentinel = new RedisSentinel(SentinelHosts);

            var redisManager = sentinel.Start();

            using (var client = redisManager.GetClient())
            {
                client.FlushAll();

                client.SetValue("Sentinel3Setup", "IntranetSentinel");

                var result = client.GetValue("Sentinel3Setup");
                Assert.That(result, Is.EqualTo("IntranetSentinel"));
            }
        }

        [Test]
        public void Can_connect_directly_to_Redis_Instances()
        {
            foreach (var host in GoogleCloudSentinelHosts)
            {
                using (var client = new RedisClient(host, 6379))
                {
                    "{0}:6379".Print(host);
                    client.Info.PrintDump();
                }

                using (var sentinel = new RedisClient(host, 26379))
                {
                    "{0}:26379".Print(host);
                    sentinel.Info.PrintDump();
                }
            }
        }

        [Test]
        public void Can_connect_to_GoogleCloud_3SentinelSetup()
        {
            var sentinel = CreateGCloudSentinel();

            var redisManager = sentinel.Start();

            using (var client = redisManager.GetClient())
            {
                "{0}:{1}".Print(client.Host, client.Port);

                client.FlushAll();

                client.SetValue("Sentinel3Setup", "GoogleCloud");

                var result = client.GetValue("Sentinel3Setup");
                Assert.That(result, Is.EqualTo("GoogleCloud"));
            }

            using (var readOnly = redisManager.GetReadOnlyClient())
            {
                "{0}:{1}".Print(readOnly.Host, readOnly.Port);

                var result = readOnly.GetValue("Sentinel3Setup");
                Assert.That(result, Is.EqualTo("GoogleCloud"));
            }
        }
    }
}