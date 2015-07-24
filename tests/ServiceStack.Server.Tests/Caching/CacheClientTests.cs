using System;
using System.Runtime.Serialization;
using ServiceStack.Caching;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Server.Tests.Shared;

namespace ServiceStack.Server.Tests.Caching
{
    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static string SqlServerBuildDb = "Server={0};Database=test;User Id=test;Password=test;".Fmt(Environment.GetEnvironmentVariable("CI_HOST"));
    }

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
    }

    public class RedisCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            return new RedisManagerPool(Environment.GetEnvironmentVariable("CI_HOST")).GetCacheClient();
        }
    }
}