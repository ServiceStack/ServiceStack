
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientSortedSetTests
	{
		private const string SetId = "testzset";

		[SetUp]
		public void SetUp()
		{
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				redis.FlushAll();
			}
		}

		[Test]
		public void Can_AddToSet_and_GetAllFromSet()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			var i = 0;
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x, i++));

				var members = redis.GetAllFromSortedSet(SetId);
				Assert.That(members.EquivalentTo(storeMembers), Is.True);
			}
		}

		[Test]
		public void AddToSet_without_score_adds_an_implicit_lexical_order_score()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				var members = redis.GetAllFromSortedSet(SetId);

				storeMembers.Sort((x, y) => x.CompareTo(y));
				Assert.That(members.EquivalentTo(storeMembers), Is.True);
			}
		}

		[Test]
		public void AddToSet_with_same_score_is_still_returned_in_lexical_order_score()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x, 1));

				var members = redis.GetAllFromSortedSet(SetId);

				storeMembers.Sort((x, y) => x.CompareTo(y));
				Assert.That(members.EquivalentTo(storeMembers), Is.True);
			}
		}

		[Test]
		public void Can_RemoveFromSet()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			const string removeMember = "two";

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				redis.RemoveFromSortedSet(SetId, removeMember);

				storeMembers.Remove(removeMember);

				var members = redis.GetAllFromSortedSet(SetId);
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_PopFromSet()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			var i = 0;
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x, i++));

				var member = redis.PopFromSortedSetItemWithHighestScore(SetId);

				Assert.That(member, Is.EqualTo("four"));
			}
		}

		[Test]
		public void Can_GetSetCount()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				var setCount = redis.GetSortedSetCount(SetId);

				Assert.That(setCount, Is.EqualTo(storeMembers.Count));
			}
		}

		[Test]
		public void Does_SortedSetContainsValue()
		{
			const string existingMember = "two";
			const string nonExistingMember = "five";
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				Assert.That(redis.SortedSetContainsValue(SetId, existingMember), Is.True);
				Assert.That(redis.SortedSetContainsValue(SetId, nonExistingMember), Is.False);
			}
		}

		[Test]
		public void Can_GetItemIndexInSortedSet_in_Asc_and_Desc()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var i = 10;
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x, i++));

				Assert.That(redis.GetItemIndexInSortedSet(SetId, "one"), Is.EqualTo(0));
				Assert.That(redis.GetItemIndexInSortedSet(SetId, "two"), Is.EqualTo(1));
				Assert.That(redis.GetItemIndexInSortedSet(SetId, "three"), Is.EqualTo(2));
				Assert.That(redis.GetItemIndexInSortedSet(SetId, "four"), Is.EqualTo(3));

				Assert.That(redis.GetItemIndexInSortedSetDesc(SetId, "one"), Is.EqualTo(3));
				Assert.That(redis.GetItemIndexInSortedSetDesc(SetId, "two"), Is.EqualTo(2));
				Assert.That(redis.GetItemIndexInSortedSetDesc(SetId, "three"), Is.EqualTo(1));
				Assert.That(redis.GetItemIndexInSortedSetDesc(SetId, "four"), Is.EqualTo(0));
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

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				set1Members.ForEach(x => redis.AddToSortedSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSortedSet(set2Name, x));

				redis.StoreIntersectFromSortedSets(storeSetName, set1Name, set2Name);

				var intersectingMembers = redis.GetAllFromSortedSet(storeSetName);

				Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
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

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				set1Members.ForEach(x => redis.AddToSortedSet(set1Name, x));
				set2Members.ForEach(x => redis.AddToSortedSet(set2Name, x));

				redis.StoreUnionFromSortedSets(storeSetName, set1Name, set2Name);

				var unionMembers = redis.GetAllFromSortedSet(storeSetName);

				Assert.That(unionMembers, Is.EquivalentTo(
					new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
			}
		}

		[Test]
		public void Can_pop_items_with_lowest_and_highest_scores_from_sorted_set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				storeMembers.Sort((x, y) => x.CompareTo(y));

				var lowestScore = redis.PopFromSortedSetItemWithLowestScore(SetId);
				Assert.That(lowestScore, Is.EqualTo(storeMembers.First()));

				var highestScore = redis.PopFromSortedSetItemWithHighestScore(SetId);
				Assert.That(highestScore, Is.EqualTo(storeMembers[storeMembers.Count - 1]));
			}
		}

		[Test]
		public void Can_GetRangeFromSortedSetByLowestScore_from_sorted_set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				storeMembers.Sort((x, y) => x.CompareTo(y));
				var memberRage = storeMembers.Where(x =>
					x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

				var range = redis.GetRangeFromSortedSetByLowestScore(SetId, "four", "three");
				Assert.That(range.EquivalentTo(memberRage));
			}
		}

		[Ignore("Not implemented yet")]
		[Test]
		public void Can_GetRangeFromSortedSetByHighestScore_from_sorted_set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				storeMembers.Sort((x, y) => y.CompareTo(x));
				var memberRage = storeMembers.Where(x =>
					x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

				var range = redis.GetRangeFromSortedSetByHighestScore(SetId, "four", "three");
				Assert.That(range.EquivalentTo(memberRage));
			}
		}

		[Test]
		public void Can_get_index_and_score_from_SortedSet()
		{
			var storeMembers = new List<string> { "a", "b", "c", "d" };
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				const double initialScore = 10d;
				var i = initialScore;
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x, i++));

				Assert.That(redis.GetItemIndexInSortedSet(SetId, "a"), Is.EqualTo(0));
				Assert.That(redis.GetItemIndexInSortedSetDesc(SetId, "a"), Is.EqualTo(storeMembers.Count - 1));

				Assert.That(redis.GetItemScoreInSortedSet(SetId, "a"), Is.EqualTo(initialScore));
				Assert.That(redis.GetItemScoreInSortedSet(SetId, "d"), Is.EqualTo(initialScore + storeMembers.Count - 1));
			}
		}

		[Test]
		public void Can_enumerate_small_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				storeMembers.ForEach(x => redis.AddToSortedSet(SetId, x));

				var members = new List<string>();
				foreach (var item in redis.SortedSets[SetId])
				{
					members.Add(item);
				}
				members.Sort();
				Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_enumerate_large_ICollection_Set()
		{
			if (TestConfig.IgnoreLongTests) return;

			const int setSize = 2500;

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var storeMembers = new List<string>();
				setSize.Times(x =>
				{
					redis.AddToSortedSet(SetId, x.ToString());
					storeMembers.Add(x.ToString());
				});

				var members = new List<string>();
				foreach (var item in redis.SortedSets[SetId])
				{
					members.Add(item);
				}
				members.Sort((x, y) => int.Parse(x).CompareTo(int.Parse(y)));
				Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_Add_to_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var list = redis.SortedSets[SetId];
				storeMembers.ForEach(list.Add);

				var members = list.ToList<string>();
				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

		[Test]
		public void Can_Clear_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var list = redis.SortedSets[SetId];
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

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var list = redis.SortedSets[SetId];
				storeMembers.ForEach(list.Add);

				Assert.That(list.Contains("two"), Is.True);
				Assert.That(list.Contains("five"), Is.False);
			}
		}

		[Test]
		public void Can_Remove_value_from_ICollection_Set()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var list = redis.SortedSets[SetId];
				storeMembers.ForEach(list.Add);

				storeMembers.Remove("two");
				list.Remove("two");

				var members = list.ToList<string>();

				Assert.That(members, Is.EquivalentTo(storeMembers));
			}
		}

	}

}