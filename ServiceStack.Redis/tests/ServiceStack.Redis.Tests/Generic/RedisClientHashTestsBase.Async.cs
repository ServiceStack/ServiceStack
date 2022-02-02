using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture, Category("Async")]
    public abstract class RedisClientHashTestsBaseAsync<T>
    {
        private const string HashId = "testhash";

        protected abstract IModelFactory<T> Factory { get; }

        private IRedisClientAsync client;
        private IRedisTypedClientAsync<T> redis;
        private IRedisHashAsync<string, T> Hash;

        [SetUp]
        public async Task SetUp()
        {
            if (client is object)
            {
                await client.DisposeAsync();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost);
            await client.FlushAllAsync();

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
        public async Task Can_SetItemInHash_and_GetAllFromHash()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var members = await redis.GetAllEntriesFromHashAsync(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public async Task Can_RemoveFromHash()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var firstKey = mapValues.First().Key;

            await redis.RemoveEntryFromHashAsync(Hash, firstKey);

            mapValues.Remove(firstKey);

            var members = await redis.GetAllEntriesFromHashAsync(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public async Task Can_GetItemFromHash()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var firstKey = mapValues.First().Key;

            var hashValue = await redis.GetValueFromHashAsync(Hash, firstKey);

            Assert.That(hashValue, Is.EqualTo(mapValues[firstKey]));
        }

        [Test]
        public async Task Can_GetHashCount()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var hashCount = await redis.GetHashCountAsync(Hash);

            Assert.That(hashCount, Is.EqualTo(mapValues.Count));
        }

        [Test]
        public async Task Does_HashContainsKey()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var existingMember = mapValues.First().Key;
            var nonExistingMember = existingMember + "notexists";

            Assert.That(await redis.HashContainsEntryAsync(Hash, existingMember), Is.True);
            Assert.That(await redis.HashContainsEntryAsync(Hash, nonExistingMember), Is.False);
        }

        [Test]
        public async Task Can_GetHashKeys()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var expectedKeys = mapValues.Map(x => x.Key);

            var hashKeys = await redis.GetHashKeysAsync(Hash);

            Assert.That(hashKeys, Is.EquivalentTo(expectedKeys));
        }

        [Test]
        public async Task Can_GetHashValues()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var expectedValues = mapValues.Map(x => x.Value);

            var hashValues = await redis.GetHashValuesAsync(Hash);

            Assert.That(hashValues, Is.EquivalentTo(expectedValues));
        }

        [Test]
        public async Task Can_enumerate_small_IDictionary_Hash()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var members = new List<string>();
            await foreach (var item in redis.GetHash<string>(HashId))
            {
                Assert.That(mapValues.ContainsKey(item.Key), Is.True);
                members.Add(item.Key);
            }
            Assert.That(members.Count, Is.EqualTo(mapValues.Count));
        }

        [Test]
        public async Task Can_Add_to_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            await mapValues.ForEachAsync((k, v) => hash.AddAsync(k, v));

            var members = await redis.GetAllEntriesFromHashAsync(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public async Task Can_Clear_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            await mapValues.ForEachAsync((k, v) => hash.AddAsync(k, v));

            Assert.That(await hash.CountAsync(), Is.EqualTo(mapValues.Count));

            await hash.ClearAsync();

            Assert.That(await hash.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            await mapValues.ForEachAsync((k, v) => hash.AddAsync(k, v));

            var existingMember = mapValues.First().Key;
            var nonExistingMember = existingMember + "notexists";

            Assert.That(await hash.ContainsKeyAsync(existingMember), Is.True);
            Assert.That(await hash.ContainsKeyAsync(nonExistingMember), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_IDictionary_Hash()
        {
            var hash = redis.GetHash<string>(HashId);
            var mapValues = CreateMap();
            await mapValues.ForEachAsync((k, v) => hash.AddAsync(k, v));

            var firstKey = mapValues.First().Key;
            mapValues.Remove(firstKey);
            await hash.RemoveAsync(firstKey);

            var members = await redis.GetAllEntriesFromHashAsync(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public async Task Can_SetItemInHashIfNotExists()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var existingMember = mapValues.First().Key;
            var nonExistingMember = existingMember + "notexists";

            var lastValue = mapValues.Last().Value;

            await redis.SetEntryInHashIfNotExistsAsync(Hash, existingMember, lastValue);
            await redis.SetEntryInHashIfNotExistsAsync(Hash, nonExistingMember, lastValue);

            mapValues[nonExistingMember] = lastValue;

            var members = await redis.GetAllEntriesFromHashAsync(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }

        [Test]
        public async Task Can_SetRangeInHash()
        {
            var mapValues = CreateMap();
            await mapValues.ForEachAsync(async (k, v) => await redis.SetEntryInHashAsync(Hash, k, v));

            var newMapValues = CreateMap2();

            await redis.SetRangeInHashAsync(Hash, newMapValues);

            newMapValues.Each(x => mapValues[x.Key] = x.Value);

            var members = await redis.GetAllEntriesFromHashAsync(Hash);
            Assert.That(members, Is.EquivalentTo(mapValues));
        }
    }

}