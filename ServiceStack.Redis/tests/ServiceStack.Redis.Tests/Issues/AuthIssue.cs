using System.Linq;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Issues
{
    public class AuthIssue
    {
        [Test]
        [Ignore("Requires password on master")]
        public void Does_retry_failed_commands_auth()
        {
            // -> Redis must have "requirepass testpassword" in config
            var connstr = "testpassword@localhost";
            RedisStats.Reset();

            var redisCtrl = new RedisClient(connstr); //RedisConfig.DefaultHost
            redisCtrl.FlushAll();
            redisCtrl.SetClient("redisCtrl");

            var redis = new RedisClient(connstr);
            redis.SetClient("redisRetry");

            var clientInfo = redisCtrl.GetClientsInfo();
            var redisId = clientInfo.First(m => m["name"] == "redisRetry")["id"];
            Assert.That(redisId.Length, Is.GreaterThan(0));

            Assert.That(redis.IncrementValue("retryCounter"), Is.EqualTo(1));

            redis.OnBeforeFlush = () =>
            {
                redisCtrl.KillClients(withId: redisId);
            };

            Assert.That(redis.IncrementValue("retryCounter"), Is.EqualTo(2));
            Assert.That(redis.Get<int>("retryCounter"), Is.EqualTo(2));

            Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(0));
        }
    }
}