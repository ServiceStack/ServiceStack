using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Server.Tests.Shared;

namespace ServiceStack.Server.Tests.Caching
{
    public class SqlServerOrmLiteCacheClientAsyncTests : CacheClientTestsAsyncBase
    {
        public override ICacheClientAsync CreateClient()
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

    public class SqliteOrmLiteCacheClientAsyncTests : CacheClientTestsAsyncBase
    {
        public override ICacheClientAsync CreateClient()
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

    public class MemoryCacheClientAsyncTests : CacheClientTestsAsyncBase
    {
        public override ICacheClientAsync CreateClient()
        {
            return new MemoryCacheClient().AsAsync();
        }

        [Test]
        public async Task Increments_are_Atomic()
        {
            var CacheClient = CreateClient();

            var numThreads = 20;
            var numIncr = 10000;
            var resetEvent = new ManualResetEvent(false);
            var threadsLeft = numThreads;

            for (var i = 0; i < numThreads; i++)
            {
                new Thread(async () =>
                {
                    for (var j = 0; j < numIncr; j++)
                    {
                        await CacheClient.IncrementAsync("test", 1);
                    }
                    if (Interlocked.Decrement(ref threadsLeft) == 0)
                        resetEvent.Set();
                }).Start();
            }

            resetEvent.WaitOne();

            Assert.That(await CacheClient.IncrementAsync("test", 0), Is.EqualTo(numThreads * numIncr));
        }
    }

    //TODO: replace with async
    public class RedisCacheClientAsyncTests : CacheClientTestsAsyncBase
    {
        public override ICacheClientAsync CreateClient()
        {
            return new RedisManagerPool("127.0.0.1").GetCacheClient().AsAsync();
        }
    }

}