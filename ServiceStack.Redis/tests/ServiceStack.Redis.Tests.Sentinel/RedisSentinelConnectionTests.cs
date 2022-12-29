using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Sentinel
{
    public class RedisSentinelConnectionTests
    {
        [Test]
        public void Can_connect_to_AWS_Redis_Sentinel_SentinelMaster()
        {
            RedisConfig.AssumeServerVersion = 4000;

            var client = new RedisClient("52.7.181.87", 26379);

            var info = client.SentinelMaster("mymaster");

            info.PrintDump();
        }

        [Test]
        public void Can_connect_to_AWS_Redis_Sentinel_Ping()
        {
            RedisConfig.AssumeServerVersion = 4000;

            var client = new RedisClient("52.7.181.87", 26379);

            Assert.That(client.Ping());
        }

        [Test]
        public void Can_connect_to_RedisSentinel()
        {
            RedisConfig.AssumeServerVersion = 4000;

            var sentinel = new RedisSentinel("52.7.181.87:26379") {
                IpAddressMap = {
                    {"127.0.0.1", "52.7.181.87"}
                }
            };

            var redisManager = sentinel.Start();

            using (var client = redisManager.GetClient())
            {
                Assert.That(client.Ping());
            }
        }
    }
}