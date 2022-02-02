using NUnit.Framework;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientHashTestsAsync
        : RedisClientTestsBaseAsync
    {
        private const string HashId = "rchtesthash";

        Dictionary<string, string> stringMap;
        Dictionary<string, int> stringIntMap;

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            stringMap = new Dictionary<string, string> {
                 {"one","a"}, {"two","b"}, {"three","c"}, {"four","d"}
             };
            stringIntMap = new Dictionary<string, int> {
                 {"one",1}, {"two",2}, {"three",3}, {"four",4}
             };
        }

        public override void OnAfterEachTest()
        {
            CleanMask = HashId + "*";
            base.OnAfterEachTest();
        }

        [Test]
        public async Task Can_SetItemInHash_and_GetAllFromHash()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public async Task Can_RemoveFromHash()
        {
            const string removeMember = "two";

            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            await RedisAsync.RemoveEntryFromHashAsync(HashId, removeMember);

            stringMap.Remove(removeMember);

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public async Task Can_GetItemFromHash()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            var hashValue = await RedisAsync.GetValueFromHashAsync(HashId, "two");

            Assert.That(hashValue, Is.EqualTo(stringMap["two"]));
        }

        [Test]
        public async Task Can_GetHashCount()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            var hashCount = await RedisAsync.GetHashCountAsync(HashId);

            Assert.That(hashCount, Is.EqualTo(stringMap.Count));
        }

        [Test]
        public async Task Does_HashContainsKey()
        {
            const string existingMember = "two";
            const string nonExistingMember = "five";

            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            Assert.That(await RedisAsync.HashContainsEntryAsync(HashId, existingMember), Is.True);
            Assert.That(await RedisAsync.HashContainsEntryAsync(HashId, nonExistingMember), Is.False);
        }

        [Test]
        public async Task Can_GetHashKeys()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }
            var expectedKeys = stringMap.Map(x => x.Key);

            var hashKeys = await RedisAsync.GetHashKeysAsync(HashId);

            Assert.That(hashKeys, Is.EquivalentTo(expectedKeys));
        }

        [Test]
        public async Task Can_GetHashValues()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }
            var expectedValues = stringMap.Map(x => x.Value);

            var hashValues = await RedisAsync.GetHashValuesAsync(HashId);

            Assert.That(hashValues, Is.EquivalentTo(expectedValues));
        }

        [Test]
        public async Task Can_enumerate_small_IDictionary_Hash()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            var members = new List<string>();
            await foreach (var item in RedisAsync.Hashes[HashId])
            {
                Assert.That(stringMap.ContainsKey(item.Key), Is.True);
                members.Add(item.Key);
            }
            Assert.That(members.Count, Is.EqualTo(stringMap.Count));
        }

        [Test]
        public async Task Can_Add_to_IDictionary_Hash()
        {
            var hash = RedisAsync.Hashes[HashId];
            foreach (var x in stringMap)
            {
                await hash.AddAsync(x);
            }

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public async Task Can_Clear_IDictionary_Hash()
        {
            var hash = RedisAsync.Hashes[HashId];
            foreach (var x in stringMap)
            {
                await hash.AddAsync(x);
            }

            Assert.That(await hash.CountAsync(), Is.EqualTo(stringMap.Count));

            await hash.ClearAsync();

            Assert.That(await hash.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_IDictionary_Hash()
        {
            var hash = RedisAsync.Hashes[HashId];
            foreach (var x in stringMap)
            {
                await hash.AddAsync(x);
            }

            Assert.That(await hash.ContainsKeyAsync("two"), Is.True);
            Assert.That(await hash.ContainsKeyAsync("five"), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_IDictionary_Hash()
        {
            var hash = RedisAsync.Hashes[HashId];
            foreach (var x in stringMap)
            {
                await hash.AddAsync(x);
            }

            stringMap.Remove("two");
            await hash.RemoveAsync("two");

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
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
        public async Task Can_increment_Hash_field()
        {
            var hash = RedisAsync.Hashes[HashId];
            foreach (var x in stringIntMap)
            {
                await hash.AddAsync(x.Key, x.Value.ToString());
            }

            stringIntMap["two"] += 10;
            await RedisAsync.IncrementValueInHashAsync(HashId, "two", 10);

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(ToStringMap(stringIntMap)));
        }

        [Test]
        public async Task Can_increment_Hash_field_beyond_32_bits()
        {
            await RedisAsync.SetEntryInHashAsync(HashId, "int", Int32.MaxValue.ToString());
            await RedisAsync.IncrementValueInHashAsync(HashId, "int", 1);
            long actual = Int64.Parse(await RedisAsync.GetValueFromHashAsync(HashId, "int"));
            long expected = Int32.MaxValue + 1L;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task Can_SetItemInHashIfNotExists()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            await RedisAsync.SetEntryInHashIfNotExistsAsync(HashId, "two", "did not change existing item");
            await RedisAsync.SetEntryInHashIfNotExistsAsync(HashId, "five", "changed non existing item");
            stringMap["five"] = "changed non existing item";

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public async Task Can_SetRangeInHash()
        {
            var newStringMap = new Dictionary<string, string> {
                 {"five","e"}, {"six","f"}, {"seven","g"}
             };
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            await RedisAsync.SetRangeInHashAsync(HashId, newStringMap);

            newStringMap.Each(x => stringMap.Add(x.Key, x.Value));

            var members = await RedisAsync.GetAllEntriesFromHashAsync(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public async Task Can_GetItemsFromHash()
        {
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashId, x.Key, x.Value);
            }

            var expectedValues = new List<string> { stringMap["one"], stringMap["two"], null };
            var hashValues = await RedisAsync.GetValuesFromHashAsync(HashId, new[] { "one", "two", "not-exists" });

            Assert.That(hashValues.EquivalentTo(expectedValues), Is.True);
        }
        [Test]
        public async Task Can_hash_set()
        {
            var key = HashId + "key";
            var field = GetBytes("foo");
            var value = GetBytes("value");
            Assert.AreEqual(await NativeAsync.HDelAsync(key, field), 0);
            Assert.AreEqual(await NativeAsync.HSetAsync(key, field, value), 1);
            Assert.AreEqual(await NativeAsync.HDelAsync(key, field), 1);
        }

        [Test]
        public async Task Can_hash_multi_set_and_get()
        {
            const string Key = HashId + "multitest";
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            var fields = new Dictionary<string, string> { { "field1", "1" }, { "field2", "2" }, { "field3", "3" } };

            await RedisAsync.SetRangeInHashAsync(Key, fields);
            var members = await RedisAsync.GetAllEntriesFromHashAsync(Key);
            foreach (var member in members)
            {
                Assert.IsTrue(fields.ContainsKey(member.Key));
                Assert.AreEqual(fields[member.Key], member.Value);
            }
        }

        public class HashTest
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public async Task Can_store_as_Hash()
        {
            var dto = new HashTest { Id = 1 };
            await RedisAsync.StoreAsHashAsync(dto);

            var storedHash = await RedisAsync.GetHashKeysAsync(dto.ToUrn());
            Assert.That(storedHash, Is.EquivalentTo(new[] { "Id" }));

            var hold = RedisClient.ConvertToHashFn;
            RedisClient.ConvertToHashFn = o =>
            {
                var map = new Dictionary<string, string>();
                o.ToObjectDictionary().Each(x => map[x.Key] = (x.Value ?? "").ToJsv());
                return map;
            };

            await RedisAsync.StoreAsHashAsync(dto);
            storedHash = await RedisAsync.GetHashKeysAsync(dto.ToUrn());
            Assert.That(storedHash, Is.EquivalentTo(new[] { "Id", "Name" }));

            RedisClient.ConvertToHashFn = hold;
        }
    }

}