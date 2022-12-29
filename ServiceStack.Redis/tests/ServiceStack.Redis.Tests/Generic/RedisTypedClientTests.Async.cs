using NUnit.Framework;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture, Category("Integration")]
    public class RedisTypedClientTestsAsync
        : RedisClientTestsBaseAsync
    {
        public class CacheRecord
        {
            public CacheRecord()
            {
                this.Children = new List<CacheRecordChild>();
            }

            public string Id { get; set; }
            public List<CacheRecordChild> Children { get; set; }
        }

        public class CacheRecordChild
        {
            public string Id { get; set; }
            public string Data { get; set; }
        }

        protected IRedisTypedClientAsync<CacheRecord> RedisTyped;

        [SetUp]
        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();

            RedisRaw?.Dispose();
            RedisRaw = new RedisClient(TestConfig.SingleHost)
            {
                NamespacePrefix = "RedisTypedClientTests:"
            };
            RedisTyped = RedisAsync.As<CacheRecord>();
        }

        [TearDown]
        public virtual async Task TearDown()
        {
            foreach (var t in await RedisAsync.SearchKeysAsync(RedisRaw.NamespacePrefix + "*"))
            {
                await NativeAsync.DelAsync(t);
            }
        }

        [Test]
        public async Task Can_Store_with_Prefix()
        {
            var expected = new CacheRecord() { Id = "123" };
            await RedisTyped.StoreAsync(expected);
            var current = await RedisAsync.GetAsync<CacheRecord>("RedisTypedClientTests:urn:cacherecord:123");
            Assert.AreEqual(expected.Id, current.Id);
        }

        [Test]
        public async Task Can_Expire()
        {
            var cachedRecord = new CacheRecord
            {
                Id = "key",
                Children = {
                    new CacheRecordChild { Id = "childKey", Data = "data" }
                }
            };

            await RedisTyped.StoreAsync(cachedRecord);
            await RedisTyped.ExpireInAsync("key", TimeSpan.FromSeconds(1));
            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Not.Null);
            await Task.Delay(2000);
            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Null);
        }

        [Ignore("Changes in system clock can break test")]
        [Test]
        public async Task Can_ExpireAt()
        {
            var cachedRecord = new CacheRecord
            {
                Id = "key",
                Children = {
                    new CacheRecordChild { Id = "childKey", Data = "data" }
                }
            };

            await RedisTyped.StoreAsync(cachedRecord);

            var in2Secs = DateTime.UtcNow.AddSeconds(2);

            await RedisTyped.ExpireAtAsync("key", in2Secs);

            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Not.Null);
            await Task.Delay(3000);
            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Null);
        }

        [Test]
        public async Task Can_Delete_All_Items()
        {
            var cachedRecord = new CacheRecord
            {
                Id = "key",
                Children = {
                    new CacheRecordChild { Id = "childKey", Data = "data" }
                }
            };

            await RedisTyped.StoreAsync(cachedRecord);

            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Not.Null);

            await RedisTyped.DeleteAllAsync();

            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Null);

        }
        
        [Test]
        public async Task Can_Delete_All_Items_multiple_batches()
        {
            // Clear previous usage
            await RedisAsync.DeleteAsync(RedisRaw.GetTypeIdsSetKey<CacheRecord>());
            var cachedRecord = new CacheRecord
            {
                Id = "key",
                Children = {
                    new CacheRecordChild { Id = "childKey", Data = "data" }
                }
            };
            
            var exists = RedisRaw.Exists(RedisRaw.GetTypeIdsSetKey(typeof(CacheRecord)));
            Assert.That(exists, Is.EqualTo(0));
            
            await RedisTyped.StoreAsync(cachedRecord);
            
            exists = RedisRaw.Exists(RedisRaw.GetTypeIdsSetKey(typeof(CacheRecord)));
            Assert.That(exists, Is.EqualTo(1));
            
            RedisConfig.CommandKeysBatchSize = 5;

            for (int i = 0; i < 50; i++)
            {
                cachedRecord.Id = "key" + i;
                await RedisTyped.StoreAsync(cachedRecord);
            }

            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Not.Null);

            await RedisTyped.DeleteAllAsync();

            Assert.That(await RedisTyped.GetByIdAsync("key"), Is.Null);
            
            exists = RedisRaw.Exists(RedisRaw.GetTypeIdsSetKey(typeof(CacheRecord)));
            Assert.That(exists, Is.EqualTo(0));

            RedisConfig.Reset();
        }
    }

}