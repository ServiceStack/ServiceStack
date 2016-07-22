using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Shared
{
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
            return Equals((Item)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }

    public class AllFields
    {
        public int Id { get; set; }
        public int? NullableId { get; set; }
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public ushort UShort { get; set; }
        public uint UInt { get; set; }
        public ulong ULong { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public string String { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public Guid Guid { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public Guid? NullableGuid { get; set; }

        protected bool Equals(AllFields other)
        {
            return Id == other.Id &&
                NullableId == other.NullableId &&
                Byte == other.Byte &&
                Short == other.Short &&
                Int == other.Int &&
                Long == other.Long &&
                UShort == other.UShort &&
                UInt == other.UInt &&
                ULong == other.ULong &&
                Float.Equals(other.Float) &&
                Double.Equals(other.Double) &&
                Decimal == other.Decimal &&
                string.Equals(String, other.String) &&
                DateTime.Equals(other.DateTime) &&
                TimeSpan.Equals(other.TimeSpan) &&
                Guid.Equals(other.Guid) &&
                NullableDateTime.Equals(other.NullableDateTime) &&
                NullableTimeSpan.Equals(other.NullableTimeSpan) &&
                NullableGuid.Equals(other.NullableGuid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AllFields)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ NullableId.GetHashCode();
                hashCode = (hashCode * 397) ^ Byte.GetHashCode();
                hashCode = (hashCode * 397) ^ Short.GetHashCode();
                hashCode = (hashCode * 397) ^ Int;
                hashCode = (hashCode * 397) ^ Long.GetHashCode();
                hashCode = (hashCode * 397) ^ UShort.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)UInt;
                hashCode = (hashCode * 397) ^ ULong.GetHashCode();
                hashCode = (hashCode * 397) ^ Float.GetHashCode();
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                hashCode = (hashCode * 397) ^ Decimal.GetHashCode();
                hashCode = (hashCode * 397) ^ (String != null ? String.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ Guid.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableTimeSpan.GetHashCode();
                hashCode = (hashCode * 397) ^ NullableGuid.GetHashCode();
                return hashCode;
            }
        }
    }


    public class CustomAuthSession : AuthUserSession
    {
        [DataMember]
        public string Custom { get; set; }
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
        public void Can_increment_and_reset_values()
        {
            Assert.That(Cache.Increment("incr:counter", 10), Is.EqualTo(10));
            Cache.Set("incr:counter", 0);
            Assert.That(Cache.Increment("incr:counter", 10), Is.EqualTo(10));
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
            Cache.Set(sessionKey, session, SessionFeature.DefaultSessionExpiry);

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

            var sessionExpiry = SessionFeature.DefaultSessionExpiry;
            Cache.Set(sessionKey, session, sessionExpiry);
            ttl = Cache.GetTimeToLive(sessionKey);
            var roundedToSec = new TimeSpan(ttl.Value.Ticks - (ttl.Value.Ticks % 1000));
            Assert.That(roundedToSec, Is.GreaterThan(TimeSpan.FromSeconds(0)));
            Assert.That(roundedToSec, Is.LessThanOrEqualTo(sessionExpiry));
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
            Cache.Set(sessionKey, session, SessionFeature.DefaultSessionExpiry);

            var sessionCache = Cache.Get<IAuthSession>(sessionKey);
            Assert.That(sessionCache, Is.Not.Null);

            var typedSession = sessionCache as CustomAuthSession;
            Assert.That(typedSession, Is.Not.Null);
            Assert.That(typedSession.Custom, Is.EqualTo("custom"));

            JsConfig.Reset();
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

            3.Times(i =>
            {
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

        [Test]
        public void Can_GetKeysByPattern()
        {
            if (!(Cache is ICacheClientExtended))
                return;

            JsConfig.ExcludeTypeInfo = true;

            5.Times(i =>
            {
                IAuthSession session = new CustomAuthSession
                {
                    Id = "sess-" + i,
                    UserAuthId = i.ToString(),
                    Custom = "custom" + i
                };

                var sessionKey = SessionFeature.GetSessionKey(session.Id);
                Cache.Set(sessionKey, session, SessionFeature.DefaultSessionExpiry);
                Cache.Set("otherkey" + i, i);
            });

            var sessionPattern = IdUtils.CreateUrn<IAuthSession>("");
            Assert.That(sessionPattern, Is.EqualTo("urn:iauthsession:"));
            var sessionKeys = Cache.GetKeysStartingWith(sessionPattern).ToList();

            Assert.That(sessionKeys.Count, Is.EqualTo(5));
            Assert.That(sessionKeys.All(x => x.StartsWith("urn:iauthsession:")));

            var allSessions = Cache.GetAll<IAuthSession>(sessionKeys);
            Assert.That(allSessions.Values.Count(x => x != null), Is.EqualTo(sessionKeys.Count));

            var allKeys = Cache.GetAllKeys().ToList();
            Assert.That(allKeys.Count, Is.EqualTo(10));

            JsConfig.Reset();
        }

        [Test]
        public void Can_Cache_AllFields()
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

            Cache.Set("allfields", dto);
            var fromCache = Cache.Get<AllFields>("allfields");

            Assert.That(fromCache.DateTime, Is.EqualTo(dto.DateTime));

            Assert.That(fromCache.Equals(dto));

            JsConfig.Reset();
        }
    }
}