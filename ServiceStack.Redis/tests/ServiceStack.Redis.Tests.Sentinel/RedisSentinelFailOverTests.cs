using System;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.Redis.Tests.Sentinel
{
    [TestFixture, Category("Integration")]
    public class RedisSentinelFailOverTests : RedisSentinelTestBase
    {
        [OneTimeSetUp]
        public void OnBeforeTestFixture()
        {
            StartAllRedisServers();
            StartAllRedisSentinels();
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled:true);
        }

        [OneTimeTearDown]
        public void OnAfterTestFixture()
        {
            ShutdownAllRedisSentinels();
            ShutdownAllRedisServers();
        }
        
        [Test]
        public void Can_Handle_First_Sentinel_Down()
        {
            var sentinel = new RedisSentinel(SentinelHosts)
            {
                RedisManagerFactory = (masters, replicas) => new PooledRedisClientManager(masters, replicas)
            };

            var redisManager = sentinel.Start();

            var cacheKey = Guid.NewGuid().ToString();

            var client = new RedisClient("127.0.0.1", SentinelPorts[0]);
            client.ShutdownNoSave();
            
            using var readOnlyClient = redisManager.GetReadOnlyClient();

            readOnlyClient.Get<long>(cacheKey);
        }
    }
}