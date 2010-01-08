using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientSetTests
	{
		private const string SetId = "testset";

		[SetUp]
		public void SetUp()
		{
			using (var redis = new RedisClient())
			{
				redis.FlushAll();
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
		public void Can_GetSetCount()
		{
			const string setId = "testsetcount";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(setId, x));

				var setCount = redis.GetSetCount(setId);

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

		[Test]
		public void Can_DiffBetweenSets()
		{
			const string set1Name = "testdiffset1";
			const string set2Name = "testdiffset2";
			const string set3Name = "testdiffset3";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };
			var set3Members = new List<string> { "one", "five", "seven", "eleven" };

			using (var redis = new RedisClient())
			{
				set1Members.ForEach(x => redis.AddToSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSet(set2Name, x));
				set3Members.ForEach(x => redis.AddToSet(set3Name, x));

				var diffMembers = redis.GetDifferencesFromSet(set1Name, set2Name, set3Name);

				Assert.That(diffMembers, Is.EquivalentTo(
											new List<string> { "two", "three" }));
			}
		}

		[Test]
		public void Can_Store_DiffBetweenSets()
		{
			const string set1Name = "testdiffset1";
			const string set2Name = "testdiffset2";
			const string set3Name = "testdiffset3";
			const string storeSetName = "testdiffsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };
			var set3Members = new List<string> { "one", "five", "seven", "eleven" };

			using (var redis = new RedisClient())
			{
				set1Members.ForEach(x => redis.AddToSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSet(set2Name, x));
				set3Members.ForEach(x => redis.AddToSet(set3Name, x));

				redis.StoreDifferencesFromSet(storeSetName, set1Name, set2Name, set3Name);

				var diffMembers = redis.GetAllFromSet(storeSetName);

				Assert.That(diffMembers, Is.EquivalentTo(
											new List<string> { "two", "three" }));
			}
		}

		[Test]
		public void Can_GetRandomEntryFromSet()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToSet(SetId, x));

				var randomEntry = redis.GetRandomEntryFromSet(SetId);

				Assert.That(storeMembers.Contains(randomEntry), Is.True);
			}
		}


		[Test]
		public void Can_enumerate_small_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(SetId, x));

				var readMembers = new List<string>();
				foreach (var item in redis.Sets[SetId])
				{
					readMembers.Add(item);
				}
				Assert.That(readMembers, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_enumerate_large_ICollection_Set()
		{
			const int setSize = 2500;

			using (var redis = new RedisClient())
			{
				var storeMembers = new List<string>();
				setSize.Times(x => {
					redis.AddToSet(SetId, x.ToString());
					storeMembers.Add(x.ToString());
				});

				var members = new List<string>();
				foreach (var item in redis.Sets[SetId])
				{
					members.Add(item);
				}
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_Add_to_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Sets[SetId];
				storeMembers.ForEach(list.Add);

				var members = list.ToList<string>();
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_Clear_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Sets[SetId];
				storeMembers.ForEach(list.Add);

				Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

				list.Clear();

				Assert.That(list.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Can_Test_Contains_in_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Sets[SetId];
				storeMembers.ForEach(list.Add);

				Assert.That(list.Contains("two"), Is.True);
				Assert.That(list.Contains("five"), Is.False);
			}
		}

		[Test]
		public void Can_Remove_value_from_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Sets[SetId];
				storeMembers.ForEach(list.Add);

				storeMembers.Remove("two");
				list.Remove("two");

				var members = list.ToList<string>();

				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

	}

}