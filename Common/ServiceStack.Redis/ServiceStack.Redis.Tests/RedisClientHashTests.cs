using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientHashTests
		: RedisClientTestsBase
	{
		private const string HashId = "testhash";

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

		[Test]
		public void Can_SetItemInHash_and_GetAllFromHash()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(stringMap));
		}

		[Test]
		public void Can_RemoveFromHash()
		{
			const string removeMember = "two";

			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			Redis.RemoveFromHash(HashId, removeMember);

			stringMap.Remove(removeMember);

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(stringMap));
		}

		[Test]
		public void Can_GetItemFromHash()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var hashValue = Redis.GetItemFromHash(HashId, "two");

			Assert.That(hashValue, Is.EqualTo(stringMap["two"]));
		}

		[Test]
		public void Can_GetHashCount()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var hashCount = Redis.GetHashCount(HashId);

			Assert.That(hashCount, Is.EqualTo(stringMap.Count));
		}

		[Test]
		public void Does_HashContainsKey()
		{
			const string existingMember = "two";
			const string nonExistingMember = "five";

			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			Assert.That(Redis.HashContainsKey(HashId, existingMember), Is.True);
			Assert.That(Redis.HashContainsKey(HashId, nonExistingMember), Is.False);
		}

		[Test]
		public void Can_GetHashKeys()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));
			var expectedKeys = stringMap.ConvertAll(x => x.Key);

			var hashKeys = Redis.GetHashKeys(HashId);

			Assert.That(hashKeys, Is.EquivalentTo(expectedKeys));
		}

		[Test]
		public void Can_GetHashValues()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));
			var expectedValues = stringMap.ConvertAll(x => x.Value);

			var hashValues = Redis.GetHashValues(HashId);

			Assert.That(hashValues, Is.EquivalentTo(expectedValues));
		}

		[Test]
		public void Can_enumerate_small_IDictionary_Hash()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

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
			stringMap.ForEach(x => hash.Add(x));

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(stringMap));
		}

		[Test]
		public void Can_Clear_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			stringMap.ForEach(x => hash.Add(x));

			Assert.That(hash.Count, Is.EqualTo(stringMap.Count));

			hash.Clear();

			Assert.That(hash.Count, Is.EqualTo(0));
		}

		[Test]
		public void Can_Test_Contains_in_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			stringMap.ForEach(x => hash.Add(x));

			Assert.That(hash.ContainsKey("two"), Is.True);
			Assert.That(hash.ContainsKey("five"), Is.False);
		}

		[Test]
		public void Can_Remove_value_from_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			stringMap.ForEach(x => hash.Add(x));

			stringMap.Remove("two");
			hash.Remove("two");

			var members = Redis.GetAllFromHash(HashId);
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
			stringIntMap.ForEach(x => hash.Add(x.Key, x.Value.ToString()));

			stringIntMap["two"] += 10;
			Redis.IncrementItemInHash(HashId, "two", 10);

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(ToStringMap(stringIntMap)));
		}

		[Test]
		public void Can_SetItemInHashIfNotExists()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			Redis.SetItemInHashIfNotExists(HashId, "two", "did not change existing item");
			Redis.SetItemInHashIfNotExists(HashId, "five", "changed non existing item");
			stringMap["five"] = "changed non existing item";

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(stringMap));
		}

		[Test]
		public void Can_SetRangeInHash()
		{
			var newStringMap = new Dictionary<string, string> {
     			{"five","e"}, {"six","f"}, {"seven","g"}
     		};
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			Redis.SetRangeInHash(HashId, newStringMap);

			newStringMap.ForEach(x => stringMap.Add(x.Key, x.Value));

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(stringMap));
		}


		[Test]
		public void Can_GetItemsFromHash()
		{
			stringMap.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var expectedValues = new List<string> { stringMap["one"], stringMap["two"], null };
			var hashValues = Redis.GetItemsFromHash(HashId, "one", "two", "not-exists");

			Assert.That(hashValues.EquivalentTo(expectedValues), Is.True);
		}

	}

}