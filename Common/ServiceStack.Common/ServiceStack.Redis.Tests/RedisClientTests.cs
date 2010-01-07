using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientTests
	{
		[Test]
		public void Can_Set_and_Get_string()
		{
			const string value = "value";
			using (var redis = new RedisClient())
			{
				redis.Set("key", value);
				var valueBytes = redis.Get("key");
				var valueString = Encoding.UTF8.GetString(valueBytes);

				Assert.That(valueString, Is.EqualTo(value));
			}
		}

		[Test]
		public void Can_AddToSet_and_GetAllFromSet()
		{
			const string setId = "testset";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(setId, x));

				var members = redis.GetAllFromSet(setId);
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_RemoveFromSet()
		{
			const string setId = "testremset";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			const string removeMember = "two";

			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(setId, x));

				redis.RemoveFromSet(setId, removeMember);

				storeMembers.Remove(removeMember);

				var members = redis.GetAllFromSet(setId);
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_PopFromSet()
		{
			const string setId = "testpopset";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(setId, x));

				var member = redis.PopFromSet(setId);

				Assert.That(storeMembers.Contains(member), Is.True);
			}
		}

		[Test]
		public void Can_MoveBetweenSets()
		{
			const string fromSetId = "testmovefromset";
			const string toSetId = "testmovetoset";
			const string moveMember = "four";
			var fromSetIdMembers = new List<string> { "one", "two", "three", "four" };
			var toSetIdMembers = new List<string> { "five", "six", "seven" };

			using (var redis = new RedisClient())
			{
				fromSetIdMembers.ForEach(x => redis.AddToSet(fromSetId, x));
				toSetIdMembers.ForEach(x => redis.AddToSet(toSetId, x));

				redis.MoveBetweenSets(fromSetId, toSetId, moveMember);

				fromSetIdMembers.Remove(moveMember);
				toSetIdMembers.Add(moveMember);

				var readFromSetId = redis.GetAllFromSet(fromSetId);
				var readToSetId = redis.GetAllFromSet(toSetId);

				Assert.That(readFromSetId, Is.EquivalentTo(fromSetIdMembers));
				Assert.That(readToSetId, Is.EquivalentTo(toSetIdMembers));
			}
		}

		[Test]
		public void Can_GetCountFromSet()
		{
			const string setId = "testsetcount";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(setId, x));

				var setCount = redis.GetCountFromSet(setId);

				Assert.That(setCount, Is.EqualTo(storeMembers.Count));
			}
		}

		[Test]
		public void Does_SetContainsValue()
		{
			const string setId = "testsetsismember";
			const string existingMember = "two";
			const string nonExistingMember = "five";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(setId, x));

				Assert.That(redis.SetContainsValue(setId, existingMember), Is.True);
				Assert.That(redis.SetContainsValue(setId, nonExistingMember), Is.False);
			}
		}

		[Test]
		public void Can_IntersectBetweenSets()
		{
			const string set1Name = "testintersectset1";
			const string set2Name = "testintersectset2";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			using (var redis = new RedisClient())
			{
				set1Members.ForEach(x => redis.AddToSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSet(set2Name, x));

				var intersectingMembers = redis.GetIntersectFromSets(set1Name, set2Name);

				Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
			}
		}

		[Test]
		public void Can_Store_IntersectBetweenSets()
		{
			const string set1Name = "testintersectset1";
			const string set2Name = "testintersectset2";
			const string storeSetName = "testintersectsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			using (var redis = new RedisClient())
			{
				set1Members.ForEach(x => redis.AddToSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSet(set2Name, x));

				redis.StoreIntersectFromSets(storeSetName, set1Name, set2Name);

				var intersectingMembers = redis.GetAllFromSet(storeSetName);

				Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
			}
		}

		[Test]
		public void Can_UnionBetweenSets()
		{
			const string set1Name = "testunionset1";
			const string set2Name = "testunionset2";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			using (var redis = new RedisClient())
			{
				set1Members.ForEach(x => redis.AddToSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSet(set2Name, x));

				var unionMembers = redis.GetUnionFromSets(set1Name, set2Name);

				Assert.That(unionMembers, Is.EquivalentTo(
					new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
			}
		}

		[Test]
		public void Can_Store_UnionBetweenSets()
		{
			const string set1Name = "testunionset1";
			const string set2Name = "testunionset2";
			const string storeSetName = "testunionsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			using (var redis = new RedisClient())
			{
				set1Members.ForEach(x => redis.AddToSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSet(set2Name, x));

				redis.StoreUnionFromSets(storeSetName, set1Name, set2Name);

				var unionMembers = redis.GetAllFromSet(storeSetName);

				Assert.That(unionMembers, Is.EquivalentTo(
					new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
			}
		}

	}

}
