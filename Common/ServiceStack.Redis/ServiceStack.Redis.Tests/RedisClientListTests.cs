using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using System.Linq;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientListTests
		: RedisClientTestsBase
	{
		const string ListId = "testlist";
		const string ListId2 = "testlist2";
		private List<string> storeMembers;

		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();
			storeMembers = new List<string> { "one", "two", "three", "four" };		
		}

		private static void AssertAreEqual(List<string> actualList, List<string> expectedList)
		{
			Assert.That(actualList, Has.Count(expectedList.Count));
			var i = 0;
			actualList.ForEach(x => Assert.That(x, Is.EqualTo(expectedList[i++])));
		}

		private static void AssertAreEqual(List<string> actualList, Queue<string> expectedList)
		{
			Assert.That(actualList, Has.Count(expectedList.Count));
			var i = 0;
			actualList.ForEach(x => Assert.That(x, Is.EqualTo(expectedList.Dequeue())));
		}

		[Test]
		public void Can_AddToList_and_GetAllFromList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var members = Redis.GetAllFromList(ListId);

			AssertAreEqual(members, storeMembers);
		}

		[Test]
		public void Can_GetListCount()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var listCount = Redis.GetListCount(ListId);

			Assert.That(listCount, Is.EqualTo(storeMembers.Count));
		}

		[Test]
		public void Can_GetItemFromList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var storeMember3 = storeMembers[2];
			var item3 = Redis.GetItemFromList(ListId, 2);

			Assert.That(item3, Is.EqualTo(storeMember3));
		}

		[Test]
		public void Can_SetItemInList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			storeMembers[2] = "five";
			Redis.SetItemInList(ListId, 2, "five");

			var members = Redis.GetAllFromList(ListId);
			AssertAreEqual(members, storeMembers);
		}

		[Test]
		public void Can_PopFromList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var item4 = Redis.PopFromList(ListId);

			Assert.That(item4, Is.EqualTo("four"));
		}

		[Test]
		public void Can_EnqueueOnList()
		{
			var queue = new Queue<string>();
			storeMembers.ForEach(queue.Enqueue);
			storeMembers.ForEach(x => Redis.EnqueueOnList(ListId, x));

			while (queue.Count > 0)
			{
				var actual = Redis.DequeueFromList(ListId);
				Assert.That(actual, Is.EqualTo(queue.Dequeue()));
			}
		}

		[Test]
		public void Can_DequeueFromList()
		{
			var queue = new Queue<string>();
			storeMembers.ForEach(queue.Enqueue);
			storeMembers.ForEach(x => Redis.EnqueueOnList(ListId, x));

			var item1 = Redis.DequeueFromList(ListId);

			Assert.That(item1, Is.EqualTo(queue.Dequeue()));
		}

		[Test]
		public void Can_BlockingDequeueFromList()
		{
			var queue = new Queue<string>();
			storeMembers.ForEach(queue.Enqueue);
			storeMembers.ForEach(x => Redis.EnqueueOnList(ListId, x));

			var item1 = Redis.BlockingDequeueFromList(ListId, null);

			Assert.That(item1, Is.EqualTo(queue.Dequeue()));
		}

		[Test]
		public void BlockingDequeueFromList_Can_TimeOut()
		{
			var item1 = Redis.BlockingDequeueFromList(ListId, TimeSpan.FromSeconds(1));
			Assert.That(item1, Is.Null);
		}

		[Test]
		public void Can_PushToList()
		{
			var stack = new Stack<string>();
			storeMembers.ForEach(stack.Push);
			storeMembers.ForEach(x => Redis.PushToList(ListId, x));

			while (stack.Count > 0)
			{
				var actual = Redis.PopFromList(ListId);
				Assert.That(actual, Is.EqualTo(stack.Pop()));
			}
		}

		[Test]
		public void Can_BlockingPopFromList()
		{
			var stack = new Stack<string>();
			storeMembers.ForEach(stack.Push);
			storeMembers.ForEach(x => Redis.PushToList(ListId, x));

			var item1 = Redis.BlockingPopFromList(ListId, null);

			Assert.That(item1, Is.EqualTo(stack.Pop()));
		}

		[Test]
		public void BlockingPopFromList_Can_TimeOut()
		{
			var item1 = Redis.BlockingPopFromList(ListId, TimeSpan.FromSeconds(1));
			Assert.That(item1, Is.Null);
		}

		[Test]
		public void Can_RemoveStartFromList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var item1 = Redis.RemoveStartFromList(ListId);

			Assert.That(item1, Is.EqualTo(storeMembers.First()));
		}

		[Test]
		public void Can_RemoveEndFromList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var item1 = Redis.RemoveEndFromList(ListId);

			Assert.That(item1, Is.EqualTo(storeMembers.Last()));
		}

		[Test]
		public void Can_BlockingRemoveStartFromList()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var item1 = Redis.BlockingRemoveStartFromList(ListId, null);

			Assert.That(item1, Is.EqualTo(storeMembers.First()));
		}

		[Test]
		public void Can_MoveBetweenLists()
		{
			var list1Members = new List<string> { "one", "two", "three", "four" };
			var list2Members = new List<string> { "five", "six", "seven" };
			const string item4 = "four";

			list1Members.ForEach(x => Redis.AddToList(ListId, x));
			list2Members.ForEach(x => Redis.AddToList(ListId2, x));

			list1Members.Remove(item4);
			list2Members.Insert(0, item4);
			Redis.PopAndPushBetweenLists(ListId, ListId2);

			var readList1 = Redis.GetAllFromList(ListId);
			var readList2 = Redis.GetAllFromList(ListId2);

			AssertAreEqual(readList1, list1Members);
			AssertAreEqual(readList2, list2Members);
		}


		[Test]
		public void Can_enumerate_small_list()
		{
			storeMembers.ForEach(x => Redis.AddToList(ListId, x));

			var readMembers = new List<string>();
			foreach (var item in Redis.Lists[ListId])
			{
				readMembers.Add(item);
			}
			AssertAreEqual(readMembers, storeMembers);
		}

		[Test]
		public void Can_enumerate_large_list()
		{
			if (TestConfig.IgnoreLongTests) return;

			const int listSize = 2500;

			storeMembers = new List<string>();
			listSize.Times(x =>
			{
				Redis.AddToList(ListId, x.ToString());
				storeMembers.Add(x.ToString());
			});

			var members = new List<string>();
			foreach (var item in Redis.Lists[ListId])
			{
				members.Add(item);
			}
			members.Sort((x, y) => int.Parse(x).CompareTo(int.Parse(y)));
			Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
			Assert.That(members, Is.EquivalentTo(storeMembers));
		}

		[Test]
		public void Can_Add_to_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			var members = list.ToList<string>();
			AssertAreEqual(members, storeMembers);
		}

		[Test]
		public void Can_Clear_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

			list.Clear();

			Assert.That(list.Count, Is.EqualTo(0));
		}

		[Test]
		public void Can_Test_Contains_in_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			Assert.That(list.Contains("two"), Is.True);
			Assert.That(list.Contains("five"), Is.False);
		}

		[Test]
		public void Can_Remove_value_from_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			storeMembers.Remove("two");
			list.Remove("two");

			var members = list.ToList<string>();

			AssertAreEqual(members, storeMembers);
		}

		[Test]
		public void Can_RemoveAt_value_from_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			storeMembers.RemoveAt(2);
			list.RemoveAt(2);

			var members = list.ToList<string>();

			AssertAreEqual(members, storeMembers);
		}

		[Test]
		public void Can_get_default_index_from_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			for (var i = 0; i < storeMembers.Count; i++)
			{
				Assert.That(list[i], Is.EqualTo(storeMembers[i]));
			}
		}

		[Test]
		public void Can_test_for_IndexOf_in_IList()
		{
			var list = Redis.Lists[ListId];
			storeMembers.ForEach(list.Add);

			foreach (var item in storeMembers)
			{
				Assert.That(list.IndexOf(item), Is.EqualTo(storeMembers.IndexOf(item)));
			}
		}


	}

}