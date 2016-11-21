using System;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Server.Tests.Shared;

namespace ServiceStack.Server.Tests.Caching
{
    public class SqlServerOrmLiteCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new OrmLiteCacheClient
            {
                DbFactory = new OrmLiteConnectionFactory(
                    Config.SqlServerBuildDb, SqlServerDialect.Provider)
            };

            using (var db = cache.DbFactory.Open())
            {
                db.DropTable<CacheEntry>();
            }

            cache.InitSchema();

            return cache;
        }
    }

    public class SqliteOrmLiteCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new OrmLiteCacheClient
            {
                DbFactory = new OrmLiteConnectionFactory(
                    ":memory:", SqliteDialect.Provider)
            };
            cache.InitSchema();

            return cache;
        }
    }

    public class MemoryCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            return new MemoryCacheClient();
        }

        [Test]
        public void Increments_are_Atomic()
        {
            var CacheClient = CreateClient();

            var numThreads = 20;
            var numIncr = 10000;
            var resetEvent = new ManualResetEvent(false);
            var threadsLeft = numThreads;

            for (var i = 0; i < numThreads; i++)
            {
                new Thread(() =>
                {
                    for (var j = 0; j < numIncr; j++)
                    {
                        CacheClient.Increment("test", 1);
                    }
                    if (Interlocked.Decrement(ref threadsLeft) == 0)
                        resetEvent.Set();
                }).Start();
            }

            resetEvent.WaitOne();

            Assert.That(CacheClient.Increment("test", 0), Is.EqualTo(numThreads * numIncr));
        }
    }

    public class RedisCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            return new RedisManagerPool("127.0.0.1").GetCacheClient();
        }
    }

    public class SqlServerMemoryOptimizedOrmLiteCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new OrmLiteCacheClient<SqlServerMemoryOptimizedCacheEntry>
            {
                DbFactory = new OrmLiteConnectionFactory(
                    Config.SqlServerBuildDb, SqlServer2014Dialect.Provider)
            };

            using (var db = cache.DbFactory.Open())
            {
                db.DropTable<SqlServerMemoryOptimizedCacheEntry>();
            }

            cache.InitSchema();

            return cache;
        }
    }

    [SqlServerMemoryOptimized(SqlServerDurability.SchemaOnly)]
    public class SqlServerMemoryOptimizedCacheEntry : ICacheEntry
    {
        [PrimaryKey]
        public string Id { get; set; }
        [StringLength(StringLengthAttribute.MaxText)]
        public string Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}