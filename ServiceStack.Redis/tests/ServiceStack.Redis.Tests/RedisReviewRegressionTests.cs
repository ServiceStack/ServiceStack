using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    /// <summary>
    /// Regression tests for issues found in the ServiceStack.Redis code-base review
    /// </summary>
    [TestFixture, Category("Integration")]
    public class RedisReviewRegressionTests
    {
        [Test]
        public void CloneClient_preserves_Ssl_and_connection_settings()
        {
            using var redis = new RedisClient(new RedisEndpoint("remote-host", 6380, "secret", 2)
            {
                Ssl = true,
                SslProtocols = SslProtocols.Tls12,
                Username = "user",
                Client = "app",
                NamespacePrefix = "ns:",
                ConnectTimeout = 1,
                SendTimeout = 2,
                ReceiveTimeout = 3,
                RetryTimeout = 4,
                IdleTimeOutSecs = 5,
            });

            using var clone = redis.CloneClient();

            Assert.That(clone.Host, Is.EqualTo("remote-host"));
            Assert.That(clone.Port, Is.EqualTo(6380));
            Assert.That(clone.Ssl, Is.True);
            Assert.That(clone.SslProtocols, Is.EqualTo(SslProtocols.Tls12));
            Assert.That(clone.Username, Is.EqualTo("user"));
            Assert.That(clone.Password, Is.EqualTo("secret"));
            Assert.That(clone.Db, Is.EqualTo(2));
            Assert.That(clone.Client, Is.EqualTo("app"));
            Assert.That(clone.NamespacePrefix, Is.EqualTo("ns:"));
            Assert.That(clone.ConnectTimeout, Is.EqualTo(1));
            Assert.That(clone.SendTimeout, Is.EqualTo(2));
            Assert.That(clone.ReceiveTimeout, Is.EqualTo(3));
            Assert.That(clone.RetryTimeout, Is.EqualTo(4));
            Assert.That(clone.IdleTimeOutSecs, Is.EqualTo(5));
        }

        [Test]
        public void RemoveByRegex_dot_plus_matches_multiple_chars()
        {
            using var redis = new RedisClient(TestConfig.SingleHost);
            redis.FlushDb();
            redis.SetValue("regex:a", "1");
            redis.SetValue("regex:abc", "1");
            redis.SetValue("regex:", "1");

            redis.RemoveByRegex("regex:.+");

            Assert.That(redis.ContainsKey("regex:a"), Is.False);
            Assert.That(redis.ContainsKey("regex:abc"), Is.False);
            Assert.That(redis.ContainsKey("regex:"), Is.True);
        }

        [Test]
        public void RemoveByPattern_removes_all_keys_across_batches()
        {
            using var redis = new RedisClient(TestConfig.SingleHost);
            redis.FlushDb();
            var map = 2500.Times(i => i).ToDictionary(i => "batch:" + i, i => i.ToString());
            redis.SetAll(map);
            redis.SetValue("other", "1");

            redis.RemoveByPattern("batch:*");

            Assert.That(redis.SearchKeys("batch:*"), Is.Empty);
            Assert.That(redis.ContainsKey("other"));
        }

        [Test]
        public async Task Async_commands_work_with_only_ReceiveTimeout_configured()
        {
            await using var redis = new RedisClient(TestConfig.SingleHost)
            {
                SendTimeout = -1,
                ReceiveTimeout = 5000,
            };
            IRedisClientAsync redisAsync = redis;

            await redisAsync.SetValueAsync("recv-timeout", "value");
            Assert.That(await redisAsync.GetValueAsync("recv-timeout"), Is.EqualTo("value"));
        }

        [Test]
        public void RedisManagerPool_GetStats_counts_created_clients()
        {
            using var pool = new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { MaxPoolSize = 4 });
            using (var redis = pool.GetClient())
                redis.Ping();

            var stats = pool.GetStats();
            Assert.That(stats["clientsPoolSize"], Is.EqualTo("4"));
            Assert.That(stats["clientsCreated"], Is.EqualTo("1"));
        }

        [Test]
        public void PooledRedisClientManager_GetStats_counts_created_clients()
        {
            using var pool = new PooledRedisClientManager(new[] { TestConfig.SingleHost }, new[] { TestConfig.SingleHost },
                new RedisClientManagerConfig { MaxWritePoolSize = 4, MaxReadPoolSize = 4 });
            using (var redis = pool.GetClient())
                redis.Ping();

            var stats = pool.GetStats();
            Assert.That(stats["writeClientsCreated"], Is.EqualTo("1"));
            Assert.That(stats["readClientsCreated"], Is.EqualTo("0"));
        }

        [Test]
        public void Newly_created_clients_do_not_reset_known_ServerVersionNumber()
        {
            using var redis = new RedisClient(TestConfig.SingleHost);
            var version = redis.AssertServerVersionNumber();
            Assert.That(version, Is.GreaterThan(0));

            using var other = new RedisClient(TestConfig.SingleHost);
            Assert.That(RedisNativeClient.ServerVersionNumber, Is.EqualTo(version));
        }

        [Test]
        public void AssertServerVersionNumber_detects_version_when_already_connected()
        {
            using var redis = new RedisClient(TestConfig.SingleHost);
            redis.Ping();
            RedisNativeClient.ServerVersionNumber = 0;

            Assert.That(redis.AssertServerVersionNumber(), Is.GreaterThan(0));
        }

        [Test]
        public void RedisPubSubServer_with_only_OnMessageBytes_receives_messages_and_filters_control_messages()
        {
            var channel = "review:pubsub:" + Guid.NewGuid().ToString("N");
            using var clientsManager = new RedisManagerPool(TestConfig.SingleHost);

            var received = new List<string>();
            var errors = new List<Exception>();
            using var allReceived = new CountdownEvent(2);
            using var subscribed = new ManualResetEventSlim();

            using var pubSub = new RedisPubSubServer(clientsManager, channel)
            {
                HeartbeatInterval = null,
                OnInit = () => subscribed.Set(),
                OnError = ex => { lock (errors) errors.Add(ex); },
                OnMessageBytes = (ch, bytes) =>
                {
                    lock (received) received.Add(bytes.FromUtf8Bytes());
                    allReceived.Signal();
                },
            }.Start();

            Assert.That(subscribed.Wait(TimeSpan.FromSeconds(5)));
            // give the bg thread time to issue SUBSCRIBE
            WaitUntil(() => { using var r = clientsManager.GetClient(); return long.Parse(r.Custom("PUBSUB", "NUMSUB", channel).Children[1].Text) > 0; });

            using (var redis = clientsManager.GetClient())
            {
                redis.PublishMessage(channel, "CTRL:PULSE"); // internal control message
                redis.PublishMessage(channel, "hello");
                redis.PublishMessage(channel, "CTRLALTDEL");   // application message that merely starts with CTRL
            }

            Assert.That(allReceived.Wait(TimeSpan.FromSeconds(5)), "Timed out waiting for messages");
            Assert.That(received, Is.EquivalentTo(new[] { "hello", "CTRLALTDEL" }));
            Assert.That(errors, Is.Empty);
        }

        private static void WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > until)
                    throw new TimeoutException();
                Thread.Sleep(20);
            }
        }
    }
}
