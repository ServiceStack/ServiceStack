using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Caching;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbCacheClientAsyncTests : CacheClientTestsAsyncBase
    {
        public override ICacheClientAsync CreateClient()
        {
            var cache = new DynamoDbCacheClient(DynamoTestBase.CreatePocoDynamo());
            cache.InitSchema();
            return cache;
        }

        [Test]
        public async Task Can_delete_expired_items_Async()
        {
            var cache = (DynamoDbCacheClient)CreateClient();
            await cache.RemoveAllAsync(await cache.Dynamo.FromScan<CacheEntry>().ExecColumnAsync(x => x.Id));

            await cache.AddAsync("expired1h", "expired", DateTime.UtcNow.AddHours(-1));
            await cache.AddAsync("expired1m", "expired", DateTime.UtcNow.AddMinutes(-1));
            await cache.AddAsync("valid1m", "valid", DateTime.UtcNow.AddMinutes(1));
            await cache.AddAsync("valid1h", "valid", DateTime.UtcNow.AddHours(1));

            await cache.RemoveExpiredEntriesAsync();

            var validEntries = (await cache.Dynamo.ScanAllAsync<CacheEntry>().ToListAsync()).Map(x => x.Id);
            Assert.That(validEntries, Is.EquivalentTo(new[] { "valid1m", "valid1h" }));
        }

        [Test]
        public async Task Can_Set_and_Get_Numeric_Value_Async()
        {
            var cache = (DynamoDbCacheClient)CreateClient();
            var val = await cache.GetAsync<int>("int");
            Assert.That(val, Is.EqualTo(default(int)));
            await cache.SetAsync("int", 1);

            val = await cache.GetAsync<int>("int");
            Assert.That(val, Is.EqualTo(1));

            await cache.SetAsync("int", "2");
            val = await cache.GetAsync<int>("int");
            Assert.That(val, Is.EqualTo(2));
        }
    }
}