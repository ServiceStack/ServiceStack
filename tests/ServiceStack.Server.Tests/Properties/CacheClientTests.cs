using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Properties
{
    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static string SqlServerBuildDb = "Server={0};Database=test;User Id=test;Password=test;".Fmt(Environment.GetEnvironmentVariable("CI_HOST"));
    }

    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; }

        protected bool Equals(Item other)
        {
            return Id == other.Id && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Item) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id*397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }

    public class CustomAuthSession : AuthUserSession
    {
        [DataMember]
        public string Custom { get; set; }
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

    [TestFixture]
    public abstract class CacheClientTestsBase
    {
        private readonly ICacheClient Cache;

        public abstract ICacheClient CreateClient();

        protected CacheClientTestsBase()
        {
            Cache = CreateClient();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            Cache.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            Cache.FlushAll();
        }

        [Test]
        public void Does_flush_all()
        {
            3.Times(i => 
                Cache.Set(i.ToUrn<Item>(), new Item { Id = i, Name = "Name" + i }));

            Assert.That(Cache.Get<Item>(1.ToUrn<Item>()), Is.Not.Null);

            Cache.FlushAll();
            
            Assert.That(Cache.Get<Item>(1.ToUrn<Item>()), Is.Null);
        }

        [Test]
        public void Can_set_and_remove_entry()
        {
            var key = 1.ToUrn<Item>();

            var item = Cache.Get<Item>(key);
            Assert.That(item, Is.Null);

            var whenNotExists = Cache.Set(key, new Item { Id = 1, Name = "Foo" });
            Assert.That(whenNotExists, Is.True);
            var whenExists = Cache.Set(key, new Item { Id = 1, Name = "Foo" });
            Assert.That(whenExists, Is.True);

            item = Cache.Get<Item>(key);
            Assert.That(item, Is.Not.Null);
            Assert.That(item.Name, Is.EqualTo("Foo"));

            whenExists = Cache.Remove(key);
            Assert.That(whenExists, Is.True);

            whenNotExists = Cache.Remove(key);
            Assert.That(whenNotExists, Is.False);
        }

        [Test]
        public void Can_update_existing_entry()
        {
            var key = 1.ToUrn<Item>();

            Cache.Set(key, new Item { Id = 1, Name = "Foo" });
            Cache.Set(key, new Item { Id = 2, Name = "Updated" });

            var item = Cache.Get<Item>(key);

            Assert.That(item.Id, Is.EqualTo(2));
            Assert.That(item.Name, Is.EqualTo("Updated"));
        }

        [Test]
        public void Does_SetAll_and_GetAll()
        {
            var map = 3.Times(i => new Item { Id = i, Name = "Name" + i })
                .ToSafeDictionary(x => x.ToUrn());

            Cache.SetAll(map);

            var cacheMap = Cache.GetAll<Item>(map.Keys);

            Assert.That(cacheMap, Is.EquivalentTo(map));
        }

        [Test]
        public void Does_not_return_expired_items()
        {
            var key = 1.ToUrn<Item>();

            Cache.Set(key, new Item { Id = 1, Name = "Foo" }, DateTime.UtcNow.AddSeconds(-1));
            Assert.That(Cache.Get<Item>(key), Is.Null);

            Cache.Remove(key);

            Cache.Set(key, new Item { Id = 1, Name = "Foo" }, TimeSpan.FromMilliseconds(100));
            var entry = Cache.Get<Item>(key);
            Assert.That(entry, Is.Not.Null);
            Thread.Sleep(200);

            Assert.That(Cache.Get<Item>(key), Is.Null);

            Cache.Remove(key);

            Cache.Set(key, new Item { Id = 1, Name = "Foo" }, DateTime.UtcNow.AddMilliseconds(200));
            entry = Cache.Get<Item>(key);
            Assert.That(entry, Is.Not.Null);
            Thread.Sleep(300);

            Assert.That(Cache.Get<Item>(key), Is.Null);
        }

        [Test]
        public void Can_increment_and_decrement_values()
        {
            Assert.That(Cache.Increment("incr:a", 2), Is.EqualTo(2));
            Assert.That(Cache.Increment("incr:a", 3), Is.EqualTo(5));

            Assert.That(Cache.Decrement("decr:a", 2), Is.EqualTo(-2));
            Assert.That(Cache.Decrement("decr:a", 3), Is.EqualTo(-5));
        }

        [Test]
        public void Can_remove_multiple_items()
        {
            var map = 5.Times(i => new Item { Id = i, Name = "Name" + i })
                .ToSafeDictionary(x => x.ToUrn());

            Cache.SetAll(map);

            Cache.RemoveAll(map.Keys);

            var cacheMap = Cache.GetAll<Item>(map.Keys);

            Assert.That(cacheMap.Count, Is.EqualTo(5));
            Assert.That(cacheMap.Values.All(x => x == null));
        }

        [Test]
        public void Can_retrieve_IAuthSession()
        {
            IAuthSession session = new CustomAuthSession
            {
                Id = "sess-1",
                UserAuthId = "1",
                Custom = "custom"
            };

            var sessionKey = SessionFeature.GetSessionKey(session.Id);
            Cache.Set(sessionKey, session, HostContext.GetDefaultSessionExpiry());

            var sessionCache = Cache.Get<IAuthSession>(sessionKey);
            Assert.That(sessionCache, Is.Not.Null);

            var typedSession = sessionCache as CustomAuthSession;
            Assert.That(typedSession, Is.Not.Null);
            Assert.That(typedSession.Custom, Is.EqualTo("custom"));
        }

        [Test]
        public void Can_retrieve_TimeToLive_on_IAuthSession()
        {
            IAuthSession session = new CustomAuthSession
            {
                Id = "sess-1",
                UserAuthId = "1",
                Custom = "custom"
            };

            var sessionKey = SessionFeature.GetSessionKey(session.Id);
            Cache.Remove(sessionKey);

            var ttl = Cache.GetTimeToLive(sessionKey);
            Assert.That(ttl, Is.Null);

            Cache.Set(sessionKey, session);
            ttl = Cache.GetTimeToLive(sessionKey);
            Assert.That(ttl.Value, Is.EqualTo(TimeSpan.MaxValue));

            var sessionExpiry = HostContext.GetDefaultSessionExpiry();
            Cache.Set(sessionKey, session, sessionExpiry);
            ttl = Cache.GetTimeToLive(sessionKey);
            Assert.That(ttl.Value, Is.GreaterThan(TimeSpan.FromSeconds(0)));
            Assert.That(ttl.Value, Is.LessThanOrEqualTo(sessionExpiry));
        }

        [Test]
        public void Can_retrieve_IAuthSession_with_global_ExcludeTypeInfo_set()
        {
            JsConfig.ExcludeTypeInfo = true;

            IAuthSession session = new CustomAuthSession
            {
                Id = "sess-1",
                UserAuthId = "1",
                Custom = "custom"
            };

            var sessionKey = SessionFeature.GetSessionKey(session.Id);
            Cache.Set(sessionKey, session, HostContext.GetDefaultSessionExpiry());

            var sessionCache = Cache.Get<IAuthSession>(sessionKey);
            Assert.That(sessionCache, Is.Not.Null);

            var typedSession = sessionCache as CustomAuthSession;
            Assert.That(typedSession, Is.Not.Null);
            Assert.That(typedSession.Custom, Is.EqualTo("custom"));

            JsConfig.ExcludeTypeInfo = false;
        }

        [Test]
        public void Can_cache_multiple_items_in_parallel()
        {
            var cache = CreateClient();
            var fns = 10.Times(i => (Action)(() =>
            {
                cache.Set("concurrent-test", "Data: {0}".Fmt(i));
            }));

            Parallel.Invoke(fns.ToArray());

            var entry = cache.Get<string>("concurrent-test");
            Assert.That(entry, Is.StringStarting("Data: "));
        }

        [Test]
        public void Can_set_get_and_remove_ISession()
        {
            var sessionA = new SessionFactory(CreateClient()).CreateSession("a");
            var sessionB = new SessionFactory(CreateClient()).CreateSession("b");

            3.Times(i => {
                sessionA.Set("key" + i, "value" + i);
                sessionB.Set("key" + i, "value" + i);
            });

            var value1 = sessionA.Get<String>("key1");
            Assert.That(value1, Is.EqualTo("value1"));

            sessionA.RemoveAll();
            value1 = sessionA.Get<String>("key1");
            Assert.That(value1, Is.Null);

            value1 = sessionB.Get<String>("key1");
            Assert.That(value1, Is.EqualTo("value1"));
        }
    }
}