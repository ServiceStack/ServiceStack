using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientSortedSetTests
		: RedisClientTestsBase
	{
		private const string SetId = "testzset";
		private List<string> storeMembers;
		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();
			storeMembers = new List<string> { "one", "two", "three", "four" };
		}

		[Test]
		public void Can_AddToSet_and_GetAllFromSet()
		{
			var i = 0;
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x, i++));

			var members = Redis.GetAllFromSortedSet(SetId);
			Assert.That(members.EquivalentTo(storeMembers), Is.True);
		}

		[Test]
		public void AddToSet_without_score_adds_an_implicit_lexical_order_score()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			var members = Redis.GetAllFromSortedSet(SetId);

			storeMembers.Sort((x, y) => x.CompareTo(y));
			Assert.That(members.EquivalentTo(storeMembers), Is.True);
		}

		[Test]
		public void AddToSet_with_same_score_is_still_returned_in_lexical_order_score()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x, 1));

			var members = Redis.GetAllFromSortedSet(SetId);

			storeMembers.Sort((x, y) => x.CompareTo(y));
			Assert.That(members.EquivalentTo(storeMembers), Is.True);
		}

		[Test]
		public void Can_RemoveFromSet()
		{
			const string removeMember = "two";

			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			Redis.RemoveFromSortedSet(SetId, removeMember);

			storeMembers.Remove(removeMember);

			var members = Redis.GetAllFromSortedSet(SetId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_PopFromSet()
		{
			var i = 0;
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x, i++));

			var member = Redis.PopFromSortedSetItemWithHighestScore(SetId);

			Assert.That(member, Is.EqualTo("four"));
		}

		[Test]
		public void Can_GetSetCount()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			var setCount = Redis.GetSortedSetCount(SetId);

			Assert.That(setCount, Is.EqualTo(storeMembers.Count));
		}

		[Test]
		public void Does_SortedSetContainsValue()
		{
			const string existingMember = "two";
			const string nonExistingMember = "five";

			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			Assert.That(Redis.SortedSetContainsValue(SetId, existingMember), Is.True);
			Assert.That(Redis.SortedSetContainsValue(SetId, nonExistingMember), Is.False);
		}

		[Test]
		public void Can_GetItemIndexInSortedSet_in_Asc_and_Desc()
		{
			var i = 10;
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x, i++));

			Assert.That(Redis.GetItemIndexInSortedSet(SetId, "one"), Is.EqualTo(0));
			Assert.That(Redis.GetItemIndexInSortedSet(SetId, "two"), Is.EqualTo(1));
			Assert.That(Redis.GetItemIndexInSortedSet(SetId, "three"), Is.EqualTo(2));
			Assert.That(Redis.GetItemIndexInSortedSet(SetId, "four"), Is.EqualTo(3));

			Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "one"), Is.EqualTo(3));
			Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "two"), Is.EqualTo(2));
			Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "three"), Is.EqualTo(1));
			Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "four"), Is.EqualTo(0));
		}

		[Test]
		public void Can_Store_IntersectBetweenSets()
		{
			const string set1Name = "testintersectset1";
			const string set2Name = "testintersectset2";
			const string storeSetName = "testintersectsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			set1Members.ForEach(x => Redis.AddToSortedSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSortedSet(set2Name, x));

			Redis.StoreIntersectFromSortedSets(storeSetName, set1Name, set2Name);

			var intersectingMembers = Redis.GetAllFromSortedSet(storeSetName);

			Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
		}

		[Test]
		public void Can_Store_UnionBetweenSets()
		{
			const string set1Name = "testunionset1";
			const string set2Name = "testunionset2";
			const string storeSetName = "testunionsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			set1Members.ForEach(x => Redis.AddToSortedSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSortedSet(set2Name, x));

			Redis.StoreUnionFromSortedSets(storeSetName, set1Name, set2Name);

			var unionMembers = Redis.GetAllFromSortedSet(storeSetName);

			Assert.That(unionMembers, Is.EquivalentTo(
				new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
		}

		[Test]
		public void Can_pop_items_with_lowest_and_highest_scores_from_sorted_set()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			storeMembers.Sort((x, y) => x.CompareTo(y));

			var lowestScore = Redis.PopFromSortedSetItemWithLowestScore(SetId);
			Assert.That(lowestScore, Is.EqualTo(storeMembers.First()));

			var highestScore = Redis.PopFromSortedSetItemWithHighestScore(SetId);
			Assert.That(highestScore, Is.EqualTo(storeMembers[storeMembers.Count - 1]));
		}

		[Test]
		public void Can_GetRangeFromSortedSetByLowestScore_from_sorted_set()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			storeMembers.Sort((x, y) => x.CompareTo(y));
			var memberRage = storeMembers.Where(x =>
				x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

			var range = Redis.GetRangeFromSortedSetByLowestScore(SetId, "four", "three");
			Assert.That(range.EquivalentTo(memberRage));
		}

		[Ignore("Not implemented yet")]
		[Test]
		public void Can_GetRangeFromSortedSetByHighestScore_from_sorted_set()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			storeMembers.Sort((x, y) => y.CompareTo(x));
			var memberRage = storeMembers.Where(x =>
				x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

			var range = Redis.GetRangeFromSortedSetByHighestScore(SetId, "four", "three");
			Assert.That(range.EquivalentTo(memberRage));
		}

		[Test]
		public void Can_get_index_and_score_from_SortedSet()
		{
			storeMembers = new List<string> { "a", "b", "c", "d" };
			const double initialScore = 10d;
			var i = initialScore;
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x, i++));

			Assert.That(Redis.GetItemIndexInSortedSet(SetId, "a"), Is.EqualTo(0));
			Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "a"), Is.EqualTo(storeMembers.Count - 1));

			Assert.That(Redis.GetItemScoreInSortedSet(SetId, "a"), Is.EqualTo(initialScore));
			Assert.That(Redis.GetItemScoreInSortedSet(SetId, "d"), Is.EqualTo(initialScore + storeMembers.Count - 1));
		}

		[Test]
		public void Can_enumerate_small_ICollection_Set()
		{
			storeMembers.ForEach(x => Redis.AddToSortedSet(SetId, x));

			var members = new List<string>();
			foreach (var item in Redis.SortedSets[SetId])
			{
				members.Add(item);
			}
			members.Sort();
			Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_enumerate_large_ICollection_Set()
		{
			if (TestConfig.IgnoreLongTests) return;

			const int setSize = 2500;

			storeMembers = new List<string>();
			setSize.Times(x =>
			{
				Redis.AddToSortedSet(SetId, x.ToString());
				storeMembers.Add(x.ToString());
			});

			var members = new List<string>();
			foreach (var item in Redis.SortedSets[SetId])
			{
				members.Add(item);
			}
			members.Sort((x, y) => int.Parse(x).CompareTo(int.Parse(y)));
			Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_Add_to_ICollection_Set()
		{
			var list = Redis.SortedSets[SetId];
			storeMembers.ForEach(list.Add);

			var members = list.ToList<string>();
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_Clear_ICollection_Set()
		{
			var list = Redis.SortedSets[SetId];
			storeMembers.ForEach(list.Add);

			Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

			list.Clear();

			Assert.That(list.Count, Is.EqualTo(0));
		}

		[Test]
		public void Can_Test_Contains_in_ICollection_Set()
		{
			var list = Redis.SortedSets[SetId];
			storeMembers.ForEach(list.Add);

			Assert.That(list.Contains("two"), Is.True);
			Assert.That(list.Contains("five"), Is.False);
		}

		[Test]
		public void Can_Remove_value_from_ICollection_Set()
		{
			var list = Redis.SortedSets[SetId];
			storeMembers.ForEach(list.Add);

			storeMembers.Remove("two");
			list.Remove("two");

			var members = list.ToList<string>();

			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

	}

}