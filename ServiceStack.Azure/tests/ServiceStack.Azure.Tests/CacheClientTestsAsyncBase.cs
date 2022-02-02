using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Azure.Tests.Shared
{
    [TestFixture]
    public abstract class CacheClientTestsAsyncBase
    {
        private readonly ICacheClientAsync Cache;

        public abstract ICacheClientAsync CreateClient();

        protected CacheClientTestsAsyncBase()
        {
            Cache = CreateClient();
        }

        [SetUp]
        public async Task SetUp()
        {
            await Cache.FlushAllAsync();
        }

        [Test]
        public async Task Does_flush_all()
        {
            await 3.TimesAsync(async i =>
                await Cache.SetAsync(i.ToUrn<Item>(), new Item { Id = i, Name = "Name" + i }));

            Assert.That(await Cache.GetAsync<Item>(1.ToUrn<Item>()), Is.Not.Null);

            await Cache.FlushAllAsync();

            Assert.That(await Cache.GetAsync<Item>(1.ToUrn<Item>()), Is.Null);
        }

        [Test]
        public async Task Can_set_and_remove_entry()
        {
            var key = 1.ToUrn<Item>();

            var item = await Cache.GetAsync<Item>(key);
            Assert.That(item, Is.Null);

            var whenNotExists = await Cache.SetAsync(key, new Item { Id = 1, Name = "Foo" });
            Assert.That(whenNotExists, Is.True);
            var whenExists = await Cache.SetAsync(key, new Item { Id = 1, Name = "Foo" });
            Assert.That(whenExists, Is.True);

            item = await Cache.GetAsync<Item>(key);
            Assert.That(item, Is.Not.Null);
            Assert.That(item.Name, Is.EqualTo("Foo"));

            whenExists = await Cache.RemoveAsync(key);
            Assert.That(whenExists, Is.True);

            whenNotExists = await Cache.RemoveAsync(key);
            Assert.That(whenNotExists, Is.False);
        }

        [Test]
        public async Task Can_update_existing_entry()
        {
            var key = 1.ToUrn<Item>();

            await Cache.SetAsync(key, new Item { Id = 1, Name = "Foo" });
            await Cache.SetAsync(key, new Item { Id = 2, Name = "Updated" });

            var item = await Cache.GetAsync<Item>(key);

            Assert.That(item.Id, Is.EqualTo(2));
            Assert.That(item.Name, Is.EqualTo("Updated"));
        }

        [Test]
        public async Task Does_SetAll_and_GetAll()
        {
            var map = 3.Times(i => new Item { Id = i, Name = "Name" + i })
                .ToSafeDictionary(x => x.ToUrn());

            await Cache.SetAllAsync(map);

            var cacheMap = await Cache.GetAllAsync<Item>(map.Keys);

            Assert.That(cacheMap, Is.EquivalentTo(map));
        }

        [Test]
        public async Task Does_not_return_expired_items()
        {
            var key = 1.ToUrn<Item>();

            await Cache.SetAsync(key, new Item { Id = 1, Name = "Foo" }, DateTime.UtcNow.AddSeconds(-1));
            Assert.That(await Cache.GetAsync<Item>(key), Is.Null);

            await Cache.RemoveAsync(key);

            await Cache.SetAsync(key, new Item { Id = 1, Name = "Foo" }, TimeSpan.FromMilliseconds(100));
            var entry = await Cache.GetAsync<Item>(key);
            Assert.That(entry, Is.Not.Null);
            Thread.Sleep(200);

            Assert.That(await Cache.GetAsync<Item>(key), Is.Null);

            await Cache.RemoveAsync(key);

            await Cache.SetAsync(key, new Item { Id = 1, Name = "Foo" }, DateTime.UtcNow.AddMilliseconds(200));
            entry = await Cache.GetAsync<Item>(key);
            Assert.That(entry, Is.Not.Null);
            Thread.Sleep(300);

            Assert.That(await Cache.GetAsync<Item>(key), Is.Null);
        }

        [Test]
        public async Task Can_increment_and_decrement_values()
        {
            Assert.That(await Cache.IncrementAsync("incr:a", 2), Is.EqualTo(2));
            Assert.That(await Cache.IncrementAsync("incr:a", 3), Is.EqualTo(5));

            Assert.That(await Cache.DecrementAsync("decr:a", 2), Is.EqualTo(-2));
            Assert.That(await Cache.DecrementAsync("decr:a", 3), Is.EqualTo(-5));
        }

        [Test]
        public async Task Can_increment_and_reset_values()
        {
            Assert.That(await Cache.IncrementAsync("incr:counter", 10), Is.EqualTo(10));
            await Cache.SetAsync("incr:counter", 0);
            Assert.That(await Cache.IncrementAsync("incr:counter", 10), Is.EqualTo(10));
        }

        [Test]
        public async Task Can_remove_multiple_items()
        {
            var map = 5.Times(i => new Item { Id = i, Name = "Name" + i })
                .ToSafeDictionary(x => x.ToUrn());

            await Cache.SetAllAsync(map);

            await Cache.RemoveAllAsync(map.Keys);

            var cacheMap = await Cache.GetAllAsync<Item>(map.Keys);

            Assert.That(cacheMap.Count, Is.EqualTo(5));
            Assert.That(cacheMap.Values.All(x => x == null));
        }

        [Test]
        public async Task Can_retrieve_IAuthSession()
        {
            IAuthSession session = new CustomAuthSession
            {
                Id = "sess-1",
                UserAuthId = "1",
                Custom = "custom"
            };

            var sessionKey = SessionFeature.GetSessionKey(session.Id);
            await Cache.SetAsync(sessionKey, session, SessionFeature.DefaultSessionExpiry);

            var sessionCache = await Cache.GetAsync<IAuthSession>(sessionKey);
            Assert.That(sessionCache, Is.Not.Null);

            var typedSession = sessionCache as CustomAuthSession;
            Assert.That(typedSession, Is.Not.Null);
            Assert.That(typedSession.Custom, Is.EqualTo("custom"));
        }

        [Test]
        public async Task Can_retrieve_TimeToLive_on_IAuthSession()
        {
            IAuthSession session = new CustomAuthSession
            {
                Id = "sess-1",
                UserAuthId = "1",
                Custom = "custom"
            };

            var sessionKey = SessionFeature.GetSessionKey(session.Id);
            await Cache.RemoveAsync(sessionKey);

            var ttl = await Cache.GetTimeToLiveAsync(sessionKey);
            Assert.That(ttl, Is.Null);

            await Cache.SetAsync(sessionKey, session);
            ttl = await Cache.GetTimeToLiveAsync(sessionKey);
            Assert.That(ttl.Value, Is.EqualTo(TimeSpan.MaxValue));

            var sessionExpiry = SessionFeature.DefaultSessionExpiry;
            await Cache.SetAsync(sessionKey, session, sessionExpiry);
            ttl = await Cache.GetTimeToLiveAsync(sessionKey);
            Assert.That(ttl.Value, Is.GreaterThan(TimeSpan.FromSeconds(0)));
            Assert.That(ttl.Value, Is.LessThan(sessionExpiry).
                                   Or.EqualTo(sessionExpiry).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public async Task Can_retrieve_IAuthSession_with_global_ExcludeTypeInfo_set()
        {
            JsConfig.ExcludeTypeInfo = true;

            IAuthSession session = new CustomAuthSession
            {
                Id = "sess-1",
                UserAuthId = "1",
                Custom = "custom"
            };

            var sessionKey = SessionFeature.GetSessionKey(session.Id);
            await Cache.SetAsync(sessionKey, session, SessionFeature.DefaultSessionExpiry);

            var sessionCache = await Cache.GetAsync<IAuthSession>(sessionKey);
            Assert.That(sessionCache, Is.Not.Null);

            var typedSession = sessionCache as CustomAuthSession;
            Assert.That(typedSession, Is.Not.Null);
            Assert.That(typedSession.Custom, Is.EqualTo("custom"));

            JsConfig.Reset();
        }

        [Test]
        public async Task Can_cache_multiple_items_in_parallel()
        {
            var cache = CreateClient();
            var fns = 10.TimesAsync(async i => 
                await cache.SetAsync("concurrent-test", "Data: {0}".Fmt(i))
            );

            await Task.WhenAll(fns);

            var entry = await cache.GetAsync<string>("concurrent-test");
            Assert.That(entry, Does.StartWith("Data: "));
        }

        [Test]
        public async Task Can_GetKeysByPattern()
        {
            if (!(Cache is ICacheClientExtended))
                return;

            JsConfig.ExcludeTypeInfo = true;

            for (int i = 0; i < 5; i++)
            {
                IAuthSession session = new CustomAuthSession
                {
                    Id = "sess-" + i,
                    UserAuthId = i.ToString(),
                    Custom = "custom" + i
                };

                var sessionKey = SessionFeature.GetSessionKey(session.Id);
                await Cache.SetAsync(sessionKey, session, SessionFeature.DefaultSessionExpiry);
                await Cache.SetAsync("otherkey" + i, i);
            }

            var sessionPattern = IdUtils.CreateUrn<IAuthSession>("");
            Assert.That(sessionPattern, Is.EqualTo("urn:iauthsession:"));
#if !NETFX            
            var sessionKeys = await Cache.GetKeysStartingWithAsync(sessionPattern).ToListAsync();

            Assert.That(sessionKeys.Count, Is.EqualTo(5));
            Assert.That(sessionKeys.All(x => x.StartsWith("urn:iauthsession:")));

            var allSessions = await Cache.GetAllAsync<IAuthSession>(sessionKeys);
            Assert.That(allSessions.Values.Count(x => x != null), Is.EqualTo(sessionKeys.Count));

            var allKeys = (await Cache.GetAllKeysAsync().ToListAsync()).ToList();
            Assert.That(allKeys.Count, Is.EqualTo(10));
#endif
            JsConfig.Reset();
        }

        [Test]
        public async Task Can_Cache_AllFields()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dto = new AllFields
            {
                Id = 1,
                NullableId = 2,
                Byte = 3,
                Short = 4,
                Int = 5,
                Long = 6,
                UShort = 7,
                UInt = 8,
                Float = 1.1f,
                Double = 2.2d,
                Decimal = 3.3m,
                String = "String",
                DateTime = DateTime.Now,
                TimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                Guid = Guid.NewGuid(),
                NullableTimeSpan = new TimeSpan(2, 2, 2),
                NullableGuid = new Guid("4B6BB8AE-57B5-4B5B-8632-0C35AF0B3168"),
            };

            await Cache.SetAsync("allfields", dto);
            var fromCache = await Cache.GetAsync<AllFields>("allfields");

            Assert.That(fromCache.DateTime, Is.EqualTo(dto.DateTime));

            Assert.That(fromCache.Equals(dto));

            JsConfig.Reset();
        }
        
        [Test]
        public async Task Can_RemoveAll_and_GetKeysStartingWith_with_prefix()
        {
            var cache = Cache.WithPrefix("prefix.");

            await cache.SetAsync("test_QUERY_Deposit__Query_Deposit_10_1", "A");
            await cache.SetAsync("test_QUERY_Deposit__0_1___CUSTOM", "B");

            var keys = (await cache.GetKeysStartingWithAsync("test_QUERY_Deposit").ToListAsync()).ToList();
            Assert.That(keys.Count, Is.EqualTo(2));

            await cache.RemoveAllAsync(keys);

            var newKeys = (await cache.GetKeysStartingWithAsync("test_QUERY_Deposit").ToListAsync()).ToList();
            Assert.That(newKeys.Count, Is.EqualTo(0));
        }

    }
}