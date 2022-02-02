using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RetryCommandTests
    {
        [Test]
        public void Does_retry_failed_commands()
        {
            // warning: this test looks brittle; is often failing "Expected: 3 But was:  2" (on main branch);

            // LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);
            // RedisConfig.EnableVerboseLogging = true;
            RedisStats.Reset();

            var redisCtrl = new RedisClient(RedisConfig.DefaultHost);
            redisCtrl.FlushAll();
            redisCtrl.SetClient("redisCtrl");

            var redis = new RedisClient(RedisConfig.DefaultHost);
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
            Assert.That(redis.Get<int>("retryCounter"), Is.EqualTo(3));

            Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(0));
        }

        [Test]
        public void Does_retry_failed_commands_with_SocketException()
        {
            RedisStats.Reset();

            var redis = new RedisClient(RedisConfig.DefaultHost);
            redis.FlushAll();

            Assert.That(redis.IncrementValue("retryCounter"), Is.EqualTo(1));

            redis.OnBeforeFlush = () =>
            {
                redis.OnBeforeFlush = null;
                throw new SocketException();
            };

            Assert.That(redis.IncrementValue("retryCounter"), Is.EqualTo(2));
            Assert.That(redis.Get<int>("retryCounter"), Is.EqualTo(3));

            Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(0));
        }

        [Test]
        public void Does_Timeout_with_repeated_SocketException()
        {
            RedisConfig.Reset();
            RedisConfig.DefaultRetryTimeout = 100;

            var redis = new RedisClient(RedisConfig.DefaultHost);
            redis.FlushAll();

            Assert.That(redis.IncrementValue("retryCounter"), Is.EqualTo(1));

            redis.OnBeforeFlush = () =>
            {
                throw new SocketException();
            };

            try
            {
                redis.IncrementValue("retryCounter");
                Assert.Fail("Should throw");
            }
            catch (RedisException ex)
            {
                Assert.That(ex.Message, Does.StartWith("Exceeded timeout"));

                redis.OnBeforeFlush = null;
                Assert.That(redis.Get<int>("retryCounter"), Is.EqualTo(1));

                Assert.That(RedisStats.TotalRetryCount, Is.GreaterThan(1));
                Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(0));
                Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(1));
            }

            RedisConfig.Reset();
        }

        [Test]
        public void Does_not_retry_when_RetryTimeout_is_Zero()
        {
            RedisConfig.Reset();
            RedisConfig.DefaultRetryTimeout = 0;

            var redis = new RedisClient(RedisConfig.DefaultHost);
            redis.FlushAll();

            Assert.That(redis.IncrementValue("retryCounter"), Is.EqualTo(1));

            redis.OnBeforeFlush = () =>
            {
                throw new SocketException();
            };

            try
            {
                redis.IncrementValue("retryCounter");
                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message, Does.StartWith("Exceeded timeout"));

                redis.OnBeforeFlush = null;
                Assert.That(redis.Get<int>("retryCounter"), Is.EqualTo(1));

                Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(0));
                Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(0));
                Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(1));
            }

            RedisConfig.Reset();
        }
    }
}