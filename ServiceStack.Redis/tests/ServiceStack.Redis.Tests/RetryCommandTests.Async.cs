using NUnit.Framework;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Async")]
    public class RetryCommandTestsAsync
    {
        [Test, Ignore("3 vs 2 needs investigation; does same in non-async")]
        public async Task Does_retry_failed_commands()
        {
            // warning: this test looks brittle; is often failing "Expected: 3 But was:  2" (on main branch);

            // LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);
            // RedisConfig.EnableVerboseLogging = true;
            RedisStats.Reset();

            var redisCtrl = new RedisClient(RedisConfig.DefaultHost).ForAsyncOnly();
            await redisCtrl.FlushAllAsync();
            await redisCtrl.SetClientAsync("redisCtrl");

            var redis = new RedisClient(RedisConfig.DefaultHost).ForAsyncOnly();
            await redis.SetClientAsync("redisRetry");

            var clientInfo = await redisCtrl.GetClientsInfoAsync();
            var redisId = clientInfo.First(m => m["name"] == "redisRetry")["id"];
            Assert.That(redisId.Length, Is.GreaterThan(0));

            Assert.That(await redis.IncrementValueAsync("retryCounter"), Is.EqualTo(1));

            ((RedisClient)redis).OnBeforeFlush = () =>
            {
                ((IRedisClient)redisCtrl).KillClients(withId: redisId);
            };

            Assert.That(await redis.IncrementValueAsync("retryCounter"), Is.EqualTo(2));
            Assert.That(await redis.GetAsync<int>("retryCounter"), Is.EqualTo(3));

            Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(0));
        }

        [Test]
        public async Task Does_retry_failed_commands_with_SocketException()
        {
            RedisStats.Reset();

            var redis = new RedisClient(RedisConfig.DefaultHost).ForAsyncOnly();
            await redis.FlushAllAsync();

            Assert.That(await redis.IncrementValueAsync("retryCounter"), Is.EqualTo(1));

            ((RedisClient)redis).OnBeforeFlush = () =>
            {
                ((RedisClient)redis).OnBeforeFlush = null;
                throw new SocketException();
            };

            Assert.That(await redis.IncrementValueAsync("retryCounter"), Is.EqualTo(2));
            Assert.That(await redis.GetAsync<int>("retryCounter"), Is.EqualTo(3));

            Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(1));
            Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(0));
        }

        [Test]
        public async Task Does_Timeout_with_repeated_SocketException()
        {
            RedisConfig.Reset();
            RedisConfig.DefaultRetryTimeout = 100;

            var redis = new RedisClient(RedisConfig.DefaultHost).ForAsyncOnly();
            await redis.FlushAllAsync();

            Assert.That(await redis.IncrementValueAsync("retryCounter"), Is.EqualTo(1));

            ((RedisClient)redis).OnBeforeFlush = () =>
            {
                throw new SocketException();
            };

            try
            {
                await redis.IncrementValueAsync("retryCounter");
                Assert.Fail("Should throw");
            }
            catch (RedisException ex)
            {
                Assert.That(ex.Message, Does.StartWith("Exceeded timeout"));

                ((RedisClient)redis).OnBeforeFlush = null;
                Assert.That(await redis.GetAsync<int>("retryCounter"), Is.EqualTo(1));

                Assert.That(RedisStats.TotalRetryCount, Is.GreaterThan(1));
                Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(0));
                Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(1));
            }

            RedisConfig.Reset();
        }

        [Test]
        public async Task Does_not_retry_when_RetryTimeout_is_Zero()
        {
            RedisConfig.Reset();
            RedisConfig.DefaultRetryTimeout = 0;

            var redis = new RedisClient(RedisConfig.DefaultHost).ForAsyncOnly();
            await redis.FlushAllAsync();

            Assert.That(await redis.IncrementValueAsync("retryCounter"), Is.EqualTo(1));

            ((RedisClient)redis).OnBeforeFlush = () =>
            {
                throw new SocketException();
            };

            try
            {
                await redis.IncrementValueAsync("retryCounter");
                Assert.Fail("Should throw");
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message, Does.StartWith("Exceeded timeout"));

                ((RedisClient)redis).OnBeforeFlush = null;
                Assert.That(await redis.GetAsync<int>("retryCounter"), Is.EqualTo(1));

                Assert.That(RedisStats.TotalRetryCount, Is.EqualTo(0));
                Assert.That(RedisStats.TotalRetrySuccess, Is.EqualTo(0));
                Assert.That(RedisStats.TotalRetryTimedout, Is.EqualTo(1));
            }

            RedisConfig.Reset();
        }
    }
}