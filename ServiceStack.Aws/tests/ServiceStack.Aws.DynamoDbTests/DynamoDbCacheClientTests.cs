using System;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Caching;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new DynamoDbCacheClient(DynamoTestBase.CreatePocoDynamo());
            cache.InitSchema();
            return cache;
        }

        [Test]
        public void Can_delete_expired_items()
        {
            var cache = (DynamoDbCacheClient)CreateClient();
            cache.RemoveAll(cache.Dynamo.FromScan<CacheEntry>().ExecColumn(x => x.Id));

            cache.Add("expired1h", "expired", DateTime.UtcNow.AddHours(-1));
            cache.Add("expired1m", "expired", DateTime.UtcNow.AddMinutes(-1));
            cache.Add("valid1m", "valid", DateTime.UtcNow.AddMinutes(1));
            cache.Add("valid1h", "valid", DateTime.UtcNow.AddHours(1));

            cache.RemoveExpiredEntries();

            var validEntries = cache.Dynamo.ScanAll<CacheEntry>().Map(x => x.Id);
            Assert.That(validEntries, Is.EquivalentTo(new[] { "valid1m", "valid1h" }));
        }

        [Test]
        public void Can_Set_and_Get_Numeric_Value()
        {
            var cache = (DynamoDbCacheClient)CreateClient();
            var val = cache.Get<int>("int");
            Assert.That(val, Is.EqualTo(default(int)));
            cache.Set("int", 1);

            val = cache.Get<int>("int");
            Assert.That(val, Is.EqualTo(1));

            cache.Set("int", "2");
            val = cache.Get<int>("int");
            Assert.That(val, Is.EqualTo(2));
        }

    }
}