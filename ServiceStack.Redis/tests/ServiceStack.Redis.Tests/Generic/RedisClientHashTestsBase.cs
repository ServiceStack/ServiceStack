using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using System.Linq;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public abstract class RedisClientHashTestsBase<T>
    {
        private const string HashId = "testhash";

        protected abstract IModelFactory<T> Factory { get; }

        private RedisClient client;
        private IRedisTypedClient<T> redis;
        private IRedisHash<string, T> Hash;

        [SetUp]
        public void SetUp()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost);
            client.FlushAll();

            redis = client.As<T>();

            Hash = redis.GetHash<string>(HashId);
        }

        private Dictionary<string, T> CreateMap()
        {
            var listValues = Factory.CreateList();
            var map = new Dictionary<string, T>();
            listValues.ForEach(x => map[x.ToString()] = x);
            return map;
        }

        private Dictionary<string, T> CreateMap2()
        {
            var listValues = Factory.CreateList2();
            var map = new Dictionary<string, T>();
            listValues.ForEach(x => map[x.ToString()] = x);
            return map;
        }

        [Test]
        public void Can_SetItemInHash_and_GetAllFromHash()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var members = redis.GetAllEntriesFromHash(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public void Can_RemoveFromHash()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var firstKey = mapValues.First().Key;

            redis.RemoveEntryFromHash(Hash, firstKey);

            mapValues.Remove(firstKey);

            var members = redis.GetAllEntriesFromHash(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public void Can_GetItemFromHash()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var firstKey = mapValues.First().Key;

            var hashValue = redis.GetValueFromHash(Hash, firstKey);

            Assert.That(hashValue, Is.EqualTo(mapValues[firstKey]));
        }

        [Test]
        public void Can_GetHashCount()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var hashCount = redis.GetHashCount(Hash);

            Assert.That(hashCount, Is.EqualTo(mapValues.Count));
        }

        [Test]
        public void Does_HashContainsKey()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var existingMember = mapValues.First().Key;
            var nonExistingMember = existingMember + "notexists";

            Assert.That(redis.HashContainsEntry(Hash, existingMember), Is.True);
            Assert.That(redis.HashContainsEntry(Hash, nonExistingMember), Is.False);
        }

        [Test]
        public void Can_GetHashKeys()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var expectedKeys = mapValues.Map(x => x.Key);

            var hashKeys = redis.GetHashKeys(Hash);

            Assert.That(hashKeys, Is.EquivalentTo(expectedKeys));
        }

        [Test]
        public void Can_GetHashValues()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var expectedValues = mapValues.Map(x => x.Value);

            var hashValues = redis.GetHashValues(Hash);

            Assert.That(hashValues, Is.EquivalentTo(expectedValues));
        }

        [Test]
        public void Can_enumerate_small_IDictionary_Hash()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var members = new List<string>();
            foreach (var item in redis.GetHash<string>(HashId))
            {
                Assert.That(mapValues.ContainsKey(item.Key), Is.True);
                members.Add(item.Key);
            }
            Assert.That(members.Count, Is.EqualTo(mapValues.Count));
        }

        [Test]
        public void Can_Add_to_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => hash.Add(k, v));

            var members = redis.GetAllEntriesFromHash(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public void Can_Clear_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => hash.Add(k, v));

            Assert.That(hash.Count, Is.EqualTo(mapValues.Count));

            hash.Clear();

            Assert.That(hash.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Test_Contains_in_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => hash.Add(k, v));

            var existingMember = mapValues.First().Key;
            var nonExistingMember = existingMember + "notexists";

            Assert.That(hash.ContainsKey(existingMember), Is.True);
            Assert.That(hash.ContainsKey(nonExistingMember), Is.False);
        }

        [Test]
        public void Can_Remove_value_from_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => hash.Add(k, v));

            var firstKey = mapValues.First().Key;
            mapValues.Remove(firstKey);
            hash.Remove(firstKey);

            var members = redis.GetAllEntriesFromHash(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        private static Dictionary<string, string> ToStringMap(Dictionary<string, int> stringIntMap)
        {
            var map = new Dictionary<string, string>();
            foreach (var kvp in stringIntMap)
            {
                map[kvp.Key] = kvp.Value.ToString();
            }
            return map;
        }

        [Test]
        public void Can_SetItemInHashIfNotExists()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var existingMember = mapValues.First().Key;
            var nonExistingMember = existingMember + "notexists";

            var lastValue = mapValues.Last().Value;

            redis.SetEntryInHashIfNotExists(Hash, existingMember, lastValue);
            redis.SetEntryInHashIfNotExists(Hash, nonExistingMember, lastValue);

            mapValues[nonExistingMember] = lastValue;

            var members = redis.GetAllEntriesFromHash(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public void Can_SetRangeInHash()
        {
            var mapValues = CreateMap();
            mapValues.ForEach((k, v) => redis.SetEntryInHash(Hash, k, v));

            var newMapValues = CreateMap2();

            redis.SetRangeInHash(Hash, newMapValues);

            newMapValues.Each(x => mapValues[x.Key] = x.Value);

            var members = redis.GetAllEntriesFromHash(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }
    }

}