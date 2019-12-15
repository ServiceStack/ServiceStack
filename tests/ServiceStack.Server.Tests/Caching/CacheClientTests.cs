using System;
using System.Threading;
using Neo4j.Driver;
using Neo4j.Driver.V1;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Caching.Neo4j;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Server.Tests.Shared;
using CacheEntry = ServiceStack.Caching.CacheEntry;
using ICacheEntry = ServiceStack.Caching.ICacheEntry;

namespace ServiceStack.Server.Tests.Caching
{
    public class SqlServerOrmLiteCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new OrmLiteCacheClient
            {
                DbFactory = new OrmLiteConnectionFactory(
                    Config.SqlServerConnString, SqlServerDialect.Provider)
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

    public class Neo4jCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new Neo4jCacheClient(GraphDatabase.Driver("bolt://localhost:7687"));
            cache.InitSchema();

            return cache;
        }

        public override void SetUp()
        {
            base.SetUp();
            
            Neo4jCacheClient.InitMappers();
        }
    }

    public class SqlServer2014MemoryOptimizedOrmLiteCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new OrmLiteCacheClient<SqlServer2014MemoryOptimizedCacheEntry>
            {
                DbFactory = new OrmLiteConnectionFactory(
                    Config.SqlServerConnString, SqlServer2014Dialect.Provider)
            };

            using (var db = cache.DbFactory.Open())
            {
                db.DropTable<SqlServer2014MemoryOptimizedCacheEntry>();
            }

            cache.InitSchema();

            return cache;
        }
    }

    [SqlServerMemoryOptimized(SqlServerDurability.SchemaOnly)]
    public class SqlServer2014MemoryOptimizedCacheEntry : ICacheEntry
    {
        [PrimaryKey]
        [SqlServerCollate("Latin1_General_100_BIN2")]
        [StringLength(512)]
        [SqlServerBucketCount(10000000)]
        public string Id { get; set; }
        [StringLength(4000)]
        public string Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class SqlServer2016MemoryOptimizedOrmLiteCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            var cache = new OrmLiteCacheClient<SqlServer2016MemoryOptimizedCacheEntry>
            {
                DbFactory = new OrmLiteConnectionFactory(
                    Config.SqlServerConnString, SqlServer2016Dialect.Provider)
            };

            using (var db = cache.DbFactory.Open())
            {
                db.DropTable<SqlServer2016MemoryOptimizedCacheEntry>();
            }

            cache.InitSchema();

            return cache;
        }
    }

    [SqlServerMemoryOptimized(SqlServerDurability.SchemaOnly)]
    public class SqlServer2016MemoryOptimizedCacheEntry : ICacheEntry
    {
        [PrimaryKey]
        [StringLength(StringLengthAttribute.MaxText)]
        [SqlServerBucketCount(10000000)]
        public string Id { get; set; }
        [StringLength(StringLengthAttribute.MaxText)]
        public string Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}