using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientSetTests
		: RedisClientTestsBase
	{
		private const string SetId = "testset";
		private List<string> storeMembers;

		[SetUp]
		public override void  OnBeforeEachTest()
		{
 			 base.OnBeforeEachTest();
			 storeMembers = new List<string> { "one", "two", "three", "four" };
		}

		[Test]
		public void Can_AddToSet_and_GetAllFromSet()
		{
			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			var members = Redis.GetAllFromSet(SetId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_RemoveFromSet()
		{
			const string removeMember = "two";

			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			Redis.RemoveFromSet(SetId, removeMember);

			storeMembers.Remove(removeMember);

			var members = Redis.GetAllFromSet(SetId);
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_PopFromSet()
		{
			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			var member = Redis.PopFromSet(SetId);

			Assert.That(storeMembers.Contains(member), Is.True);
		}

		[Test]
		public void Can_MoveBetweenSets()
		{
			const string fromSetId = "testmovefromset";
			const string toSetId = "testmovetoset";
			const string moveMember = "four";
			var fromSetIdMembers = new List<string> { "one", "two", "three", "four" };
			var toSetIdMembers = new List<string> { "five", "six", "seven" };

			fromSetIdMembers.ForEach(x => Redis.AddToSet(fromSetId, x));
			toSetIdMembers.ForEach(x => Redis.AddToSet(toSetId, x));

			Redis.MoveBetweenSets(fromSetId, toSetId, moveMember);

			fromSetIdMembers.Remove(moveMember);
			toSetIdMembers.Add(moveMember);

			var readFromSetId = Redis.GetAllFromSet(fromSetId);
			var readToSetId = Redis.GetAllFromSet(toSetId);

			Assert.That(readFromSetId, Is.EquivalentTo(fromSetIdMembers));
			Assert.That(readToSetId, Is.EquivalentTo(toSetIdMembers));
		}

		[Test]
		public void Can_GetSetCount()
		{
			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			var setCount = Redis.GetSetCount(SetId);

			Assert.That(setCount, Is.EqualTo(storeMembers.Count));
		}

		[Test]
		public void Does_SetContainsValue()
		{
			const string existingMember = "two";
			const string nonExistingMember = "five";

			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			Assert.That(Redis.SetContainsValue(SetId, existingMember), Is.True);
			Assert.That(Redis.SetContainsValue(SetId, nonExistingMember), Is.False);
		}

		[Test]
		public void Can_IntersectBetweenSets()
		{
			const string set1Name = "testintersectset1";
			const string set2Name = "testintersectset2";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			set1Members.ForEach(x => Redis.AddToSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSet(set2Name, x));

			var intersectingMembers = Redis.GetIntersectFromSets(set1Name, set2Name);

			Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
		}

		[Test]
		public void Can_Store_IntersectBetweenSets()
		{
			const string set1Name = "testintersectset1";
			const string set2Name = "testintersectset2";
			const string storeSetName = "testintersectsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			set1Members.ForEach(x => Redis.AddToSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSet(set2Name, x));

			Redis.StoreIntersectFromSets(storeSetName, set1Name, set2Name);

			var intersectingMembers = Redis.GetAllFromSet(storeSetName);

			Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
		}

		[Test]
		public void Can_UnionBetweenSets()
		{
			const string set1Name = "testunionset1";
			const string set2Name = "testunionset2";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			set1Members.ForEach(x => Redis.AddToSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSet(set2Name, x));

			var unionMembers = Redis.GetUnionFromSets(set1Name, set2Name);

			Assert.That(unionMembers, Is.EquivalentTo(
				new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
		}

		[Test]
		public void Can_Store_UnionBetweenSets()
		{
			const string set1Name = "testunionset1";
			const string set2Name = "testunionset2";
			const string storeSetName = "testunionsetstore";
			var set1Members = new List<string> { "one", "two", "three", "four", "five" };
			var set2Members = new List<string> { "four", "five", "six", "seven" };

			set1Members.ForEach(x => Redis.AddToSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSet(set2Name, x));

			Redis.StoreUnionFromSets(storeSetName, set1Name, set2Name);

			var unionMembers = Redis.GetAllFromSet(storeSetName);

			Assert.That(unionMembers, Is.EquivalentTo(
				new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
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

			set1Members.ForEach(x => Redis.AddToSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSet(set2Name, x));
			set3Members.ForEach(x => Redis.AddToSet(set3Name, x));

			var diffMembers = Redis.GetDifferencesFromSet(set1Name, set2Name, set3Name);

			Assert.That(diffMembers, Is.EquivalentTo(
				new List<string> { "two", "three" }));
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

			set1Members.ForEach(x => Redis.AddToSet(set1Name, x));
			set2Members.ForEach(x => Redis.AddToSet(set2Name, x));
			set3Members.ForEach(x => Redis.AddToSet(set3Name, x));

			Redis.StoreDifferencesFromSet(storeSetName, set1Name, set2Name, set3Name);

			var diffMembers = Redis.GetAllFromSet(storeSetName);

			Assert.That(diffMembers, Is.EquivalentTo(
				new List<string> { "two", "three" }));
		}

		[Test]
		public void Can_GetRandomEntryFromSet()
		{
			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			var randomEntry = Redis.GetRandomEntryFromSet(SetId);

			Assert.That(storeMembers.Contains(randomEntry), Is.True);
		}


		[Test]
		public void Can_enumerate_small_ICollection_Set()
		{
			storeMembers.ForEach(x => Redis.AddToSet(SetId, x));

			var members = new List<string>();
			foreach (var item in Redis.Sets[SetId])
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
				Redis.AddToSet(SetId, x.ToString());
				storeMembers.Add(x.ToString());
			});

			var members = new List<string>();
			foreach (var item in Redis.Sets[SetId])
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
			var list = Redis.Sets[SetId];
			storeMembers.ForEach(list.Add);

			var members = list.ToList<string>();
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_Clear_ICollection_Set()
		{
			var list = Redis.Sets[SetId];
			storeMembers.ForEach(list.Add);

			Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

			list.Clear();

			Assert.That(list.Count, Is.EqualTo(0));
		}

		[Test]
		public void Can_Test_Contains_in_ICollection_Set()
		{
			var list = Redis.Sets[SetId];
			storeMembers.ForEach(list.Add);

			Assert.That(list.Contains("two"), Is.True);
			Assert.That(list.Contains("five"), Is.False);
		}

		[Test]
		public void Can_Remove_value_from_ICollection_Set()
		{
			var list = Redis.Sets[SetId];
			storeMembers.ForEach(list.Add);

			storeMembers.Remove("two");
			list.Remove("two");

			var members = list.ToList<string>();

			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

	}

}