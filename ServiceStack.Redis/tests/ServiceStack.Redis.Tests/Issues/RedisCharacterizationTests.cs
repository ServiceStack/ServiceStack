using System;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Issues
{
    public class RedisCharacterizationTests
    {
        private IRedisClientsManager _db1ClientManager;
        private IRedisClientsManager _db2ClientManager;

        [SetUp]
        public void SetUp()
        {
            foreach (var clientManager in new[] { _db1ClientManager, _db2ClientManager })
            {
                if (clientManager != null)
                {
                    using (var cacheClient = clientManager.GetCacheClient())
                    {
                        cacheClient.Remove("key");
                    }
                }
            }
        }

        [Test]
        public void BasicRedisClientManager_WhenUsingADatabaseOnARedisConnectionString_CorrectDatabaseIsUsed()
        {
            TestForDatabaseOnConnectionString(connectionString => new BasicRedisClientManager(connectionString));
        }

        [Test]
        public void PooledRedisClientManager_WhenUsingADatabaseOnARedisConnectionString_CorrectDatabaseIsUsed()
        {
            TestForDatabaseOnConnectionString(connectionString => new PooledRedisClientManager(connectionString));
        }

        [Test]
        public void RedisManagerPool_WhenUsingADatabaseOnARedisConnectionString_CorrectDatabaseIsUsed()
        {
            TestForDatabaseOnConnectionString(connectionString => new RedisManagerPool(connectionString));
        }

        private void TestForDatabaseOnConnectionString(Func<string, IRedisClientsManager> factory)
        {
            _db1ClientManager = factory(TestConfig.SingleHost + "?db=1");
            _db2ClientManager = factory(TestConfig.SingleHost + "?db=2");

            using (var cacheClient = _db1ClientManager.GetCacheClient())
            {
                cacheClient.Set("key", "value");
            }
            using (var cacheClient = _db2ClientManager.GetCacheClient())
            {
                Assert.Null(cacheClient.Get<string>("key"));
            }
        }

        [Test]
        public void WhenUsingAnInitialDatabase_CorrectDatabaseIsUsed()
        {
            _db1ClientManager = new BasicRedisClientManager(1, TestConfig.SingleHost);
            _db2ClientManager = new BasicRedisClientManager(2, TestConfig.SingleHost);

            using (var cacheClient = _db1ClientManager.GetCacheClient())
            {
                cacheClient.Set("key", "value");
            }
            using (var cacheClient = _db2ClientManager.GetCacheClient())
            {
                Assert.Null(cacheClient.Get<string>("key"));
            }
        }
    }
}