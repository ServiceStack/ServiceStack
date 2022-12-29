using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Tests.Generic;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientHashTests
        : RedisClientTestsBase
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
        public void Can_SetItemInHash_and_GetAllFromHash()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            var members = Redis.GetAllEntriesFromHash(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public void Can_RemoveFromHash()
        {
            const string removeMember = "two";

            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            Redis.RemoveEntryFromHash(HashId, removeMember);

            stringMap.Remove(removeMember);

            var members = Redis.GetAllEntriesFromHash(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public void Can_GetItemFromHash()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            var hashValue = Redis.GetValueFromHash(HashId, "two");

            Assert.That(hashValue, Is.EqualTo(stringMap["two"]));
        }

        [Test]
        public void Can_GetHashCount()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            var hashCount = Redis.GetHashCount(HashId);

            Assert.That(hashCount, Is.EqualTo(stringMap.Count));
        }

        [Test]
        public void Does_HashContainsKey()
        {
            const string existingMember = "two";
            const string nonExistingMember = "five";

            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            Assert.That(Redis.HashContainsEntry(HashId, existingMember), Is.True);
            Assert.That(Redis.HashContainsEntry(HashId, nonExistingMember), Is.False);
        }

        [Test]
        public void Can_GetHashKeys()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));
            var expectedKeys = stringMap.Map(x => x.Key);

            var hashKeys = Redis.GetHashKeys(HashId);

            Assert.That(hashKeys, Is.EquivalentTo(expectedKeys));
        }

        [Test]
        public void Can_GetHashValues()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));
            var expectedValues = stringMap.Map(x => x.Value);

            var hashValues = Redis.GetHashValues(HashId);

            Assert.That(hashValues, Is.EquivalentTo(expectedValues));
        }

        [Test]
        public void Can_enumerate_small_IDictionary_Hash()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            var members = new List<string>();
            foreach (var item in Redis.Hashes[HashId])
            {
                Assert.That(stringMap.ContainsKey(item.Key), Is.True);
                members.Add(item.Key);
            }
            Assert.That(members.Count, Is.EqualTo(stringMap.Count));
        }

        [Test]
        public void Can_Add_to_IDictionary_Hash()
        {
            var hash = Redis.Hashes[HashId];
            stringMap.Each(hash.Add);

            var members = Redis.GetAllEntriesFromHash(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public void Can_Clear_IDictionary_Hash()
        {
            var hash = Redis.Hashes[HashId];
            stringMap.Each(hash.Add);

            Assert.That(hash.Count, Is.EqualTo(stringMap.Count));

            hash.Clear();

            Assert.That(hash.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Test_Contains_in_IDictionary_Hash()
        {
            var hash = Redis.Hashes[HashId];
            stringMap.Each(hash.Add);

            Assert.That(hash.ContainsKey("two"), Is.True);
            Assert.That(hash.ContainsKey("five"), Is.False);
        }

        [Test]
        public void Can_Remove_value_from_IDictionary_Hash()
        {
            var hash = Redis.Hashes[HashId];
            stringMap.Each(hash.Add);

            stringMap.Remove("two");
            hash.Remove("two");

            var members = Redis.GetAllEntriesFromHash(HashId);
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
        public void Can_increment_Hash_field()
        {
            var hash = Redis.Hashes[HashId];
            stringIntMap.Each(x => hash.Add(x.Key, x.Value.ToString()));

            stringIntMap["two"] += 10;
            Redis.IncrementValueInHash(HashId, "two", 10);

            var members = Redis.GetAllEntriesFromHash(HashId);
            Assert.That(members, Is.EquivalentTo(ToStringMap(stringIntMap)));
        }

        [Test]
        public void Can_increment_Hash_field_beyond_32_bits()
        {
            Redis.SetEntryInHash(HashId, "int", Int32.MaxValue.ToString());
            Redis.IncrementValueInHash(HashId, "int", 1);
            long actual = Int64.Parse(Redis.GetValueFromHash(HashId, "int"));
            long expected = Int32.MaxValue + 1L;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Can_SetItemInHashIfNotExists()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            Redis.SetEntryInHashIfNotExists(HashId, "two", "did not change existing item");
            Redis.SetEntryInHashIfNotExists(HashId, "five", "changed non existing item");
            stringMap["five"] = "changed non existing item";

            var members = Redis.GetAllEntriesFromHash(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public void Can_SetRangeInHash()
        {
            var newStringMap = new Dictionary<string, string> {
                 {"five","e"}, {"six","f"}, {"seven","g"}
             };
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            Redis.SetRangeInHash(HashId, newStringMap);

            newStringMap.Each(x => stringMap.Add(x.Key, x.Value));

            var members = Redis.GetAllEntriesFromHash(HashId);
            Assert.That(members, Is.EquivalentTo(stringMap));
        }

        [Test]
        public void Can_GetItemsFromHash()
        {
            stringMap.Each(x => Redis.SetEntryInHash(HashId, x.Key, x.Value));

            var expectedValues = new List<string> { stringMap["one"], stringMap["two"], null };
            var hashValues = Redis.GetValuesFromHash(HashId, "one", "two", "not-exists");

            Assert.That(hashValues.EquivalentTo(expectedValues), Is.True);
        }
        [Test]
        public void Can_hash_set()
        {
            var key = HashId + "key";
            var field = GetBytes("foo");
            var value = GetBytes("value");
            Assert.AreEqual(Redis.HDel(key, field), 0);
            Assert.AreEqual(Redis.HSet(key, field, value), 1);
            Assert.AreEqual(Redis.HDel(key, field), 1);
        }

        [Test]
        public void Can_hash_multi_set_and_get()
        {
            const string Key = HashId + "multitest";
            Assert.That(Redis.GetValue(Key), Is.Null);
            var fields = new Dictionary<string, string> { { "field1", "1" }, { "field2", "2" }, { "field3", "3" } };

            Redis.SetRangeInHash(Key, fields);
            var members = Redis.GetAllEntriesFromHash(Key);
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
        public void Can_store_as_Hash()
        {
            var dto = new HashTest { Id = 1 };
            Redis.StoreAsHash(dto);

            var storedHash = Redis.GetHashKeys(dto.ToUrn());
            Assert.That(storedHash, Is.EquivalentTo(new[] { "Id" }));

            var hold = RedisClient.ConvertToHashFn;
            RedisClient.ConvertToHashFn = o =>
            {
                var map = new Dictionary<string, string>();
                o.ToObjectDictionary().Each(x => map[x.Key] = (x.Value ?? "").ToJsv());
                return map;
            };

            Redis.StoreAsHash(dto);
            storedHash = Redis.GetHashKeys(dto.ToUrn());
            Assert.That(storedHash, Is.EquivalentTo(new[] { "Id", "Name" }));

            RedisClient.ConvertToHashFn = hold;
        }
    }

}