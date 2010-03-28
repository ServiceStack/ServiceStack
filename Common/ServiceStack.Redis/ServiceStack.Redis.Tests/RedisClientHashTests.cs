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

		Dictionary<string, string> storeMembers;

		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();
			storeMembers = new Dictionary<string, string> {
     			{"one","a"}, {"two","b"}, {"three","c"}, {"four","d"}
     		};
		}

		[Test]
		public void Can_SetItemInHash_and_GetAllFromHash()
		{
			storeMembers.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_RemoveFromHash()
		{
			const string removeMember = "two";

			storeMembers.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			Redis.RemoveFromHash(HashId, removeMember);

			storeMembers.Remove(removeMember);

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_GetHashCount()
		{
			storeMembers.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var hashCount = Redis.GetHashCount(HashId);

			Assert.That(hashCount, Is.EqualTo(storeMembers.Count));
		}

		[Test]
		public void Does_HashContainsKey()
		{
			const string existingMember = "two";
			const string nonExistingMember = "five";

			storeMembers.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			Assert.That(Redis.HashContainsKey(HashId, existingMember), Is.True);
			Assert.That(Redis.HashContainsKey(HashId, nonExistingMember), Is.False);
		}

		[Test]
		public void Can_enumerate_small_IDictionary_Hash()
		{
			storeMembers.ForEach(x => Redis.SetItemInHash(HashId, x.Key, x.Value));

			var members = new List<string>();
			foreach (var item in Redis.Hashes[HashId])
			{
				Assert.That(storeMembers.ContainsKey(item.Key), Is.True);
				members.Add(item.Key);
			}
			Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
		}

		[Test]
		public void Can_Add_to_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			storeMembers.ForEach(x => hash.Add(x));

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_Clear_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			storeMembers.ForEach(x => hash.Add(x));

			Assert.That(hash.Count, Is.EqualTo(storeMembers.Count));

			hash.Clear();

			Assert.That(hash.Count, Is.EqualTo(0));
		}

		[Test]
		public void Can_Test_Contains_in_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			storeMembers.ForEach(x => hash.Add(x));

			Assert.That(hash.ContainsKey("two"), Is.True);
			Assert.That(hash.ContainsKey("five"), Is.False);
		}

		[Test]
		public void Can_Remove_value_from_IDictionary_Hash()
		{
			var hash = Redis.Hashes[HashId];
			storeMembers.ForEach(x => hash.Add(x));

			storeMembers.Remove("two");
			hash.Remove("two");

			var members = Redis.GetAllFromHash(HashId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

	}

}