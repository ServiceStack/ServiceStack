using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientListTests
	{
		const string ListId = "testlist";
		const string ListId2 = "testlist2";

		private static void AssertListsAreEqual(List<string> actualList, List<string> expectedList)
		{
			Assert.That(actualList, Has.Count(expectedList.Count));
			var i = 0;
			actualList.ForEach(x => Assert.That(x, Is.EqualTo(expectedList[i++])));
		}

		[SetUp]
		public void SetUp()
		{
			using (var redis = new RedisClient())
			{
				redis.FlushAll();
			}			
		}

		[Test]
		public void Can_AddToList_and_GetAllFromList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var members = redis.GetAllFromList(ListId);

				AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_GetListCount()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var listCount = redis.GetListCount(ListId);

				Assert.That(listCount, Is.EqualTo(storeMembers.Count));
			}
		}

		[Test]
		public void Can_GetItemFromList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var storeMember3 = storeMembers[2];
				var item3 = redis.GetItemFromList(ListId, 2);

				Assert.That(item3, Is.EqualTo(storeMember3));
			}
		}

		[Test]
		public void Can_SetItemInList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				storeMembers[2] = "five";
				redis.SetItemInList(ListId, 2, "five");

				var members = redis.GetAllFromList(ListId);

				AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_PopFromList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var item4 = redis.PopFromList(ListId);

				Assert.That(item4, Is.EqualTo("four"));
			}
		}

		[Test]
		public void Can_DequeueFromList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };
			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var item1 = redis.DequeueFromList(ListId);

				Assert.That(item1, Is.EqualTo("one"));
			}
		}

		[Test]
		public void Can_MoveBetweenLists()
		{
			var list1Members = new List<string> { "one", "two", "three", "four" };
			var list2Members = new List<string> { "five", "six", "seven" };
			const string item4 = "four";

			using (var redis = new RedisClient())
			{
				list1Members.ForEach(x => redis.AddToList(ListId, x));
				list2Members.ForEach(x => redis.AddToList(ListId2, x));

				list1Members.Remove(item4);
				list2Members.Insert(0, item4);
				redis.PopAndPushBetweenLists(ListId, ListId2);

				var readList1 = redis.GetAllFromList(ListId);
				var readList2 = redis.GetAllFromList(ListId2);

				AssertListsAreEqual(readList1, list1Members);
				AssertListsAreEqual(readList2, list2Members);
			}
		}


		[Test]
		public void Can_enumerate_small_list()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var readMembers = new List<string>();
				foreach (var item in redis.Lists[ListId])
				{
					readMembers.Add(item);
				}
				AssertListsAreEqual(readMembers, storeMembers);
			}
		}

		[Test]
		public void Can_enumerate_large_list()
		{
			const int listSize = 2500;

			using (var redis = new RedisClient())
			{
				listSize.Times(x => redis.AddToList(ListId, x.ToString()));

				var i = 0;
				foreach (var item in redis.Lists[ListId])
				{
					Assert.That(item, Is.EqualTo(i++.ToString()));
				}
			}
		}

		[Test]
		public void Can_Add_to_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				var members = list.ToList<string>();
				AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_Clear_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

				list.Clear();

				Assert.That(list.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Can_Test_Contains_in_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				Assert.That(list.Contains("two"), Is.True);
				Assert.That(list.Contains("five"), Is.False);
			}
		}

		[Test]
		public void Can_Remove_value_from_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				storeMembers.Remove("two");
				list.Remove("two");

				var members = list.ToList<string>();

				AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_RemoveAt_value_from_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				storeMembers.RemoveAt(2);
				list.RemoveAt(2);

				var members = list.ToList<string>();

				AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_get_default_index_from_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				for (var i=0; i < storeMembers.Count; i++)
				{
					Assert.That(list[i], Is.EqualTo(storeMembers[i]));
				}
			}
		}

		[Test]
		public void Can_test_for_IndexOf_in_IList()
		{
			var storeMembers = new List<string> { "one", "two", "three", "four" };

			using (var redis = new RedisClient())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				foreach (var item in storeMembers)
				{
					Assert.That(list.IndexOf(item), Is.EqualTo(storeMembers.IndexOf(item)));
				}
			}
		}


	}

}