using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
	[TestFixture]
	public abstract class RedisClientListTestsBase<T>
	{
		const string ListId = "testlist";
		const string ListId2 = "testlist2";

		protected abstract IModelFactory<T> Factory { get; }

		[SetUp]
		public void SetUp()
		{
			using (var redis = new RedisGenericClient<T>())
			{
				redis.FlushAll();
			}			
		}

		[Test]
		public void Can_AddToList_and_GetAllFromList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var members = redis.GetAllFromList(ListId);

				Factory.AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_GetListCount()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var listCount = redis.GetListCount(ListId);

				Assert.That(listCount, Is.EqualTo(storeMembers.Count));
			}
		}

		[Test]
		public void Can_GetItemFromList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var storeMember3 = storeMembers[2];
				var item3 = redis.GetItemFromList(ListId, 2);

				Factory.AssertIsEqual(item3, storeMember3);
			}
		}

		[Test]
		public void Can_SetItemInList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				storeMembers[2] = Factory.NonExistingValue;
				redis.SetItemInList(ListId, 2, Factory.NonExistingValue);

				var members = redis.GetAllFromList(ListId);

				Factory.AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_PopFromList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var lastValue = redis.PopFromList(ListId);

				Factory.AssertIsEqual(lastValue, storeMembers[storeMembers.Count - 1]);
			}
		}

		[Test]
		public void Can_DequeueFromList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var item1 = redis.DequeueFromList(ListId);

				Factory.AssertIsEqual(item1, (T) storeMembers.First());
			}
		}

		[Test][Ignore("Makes redis 1.2 hang")]
		public void Can_MoveBetweenLists()
		{
			var list1Members = Factory.CreateList();
			var list2Members = Factory.CreateList2();
			var lastItem = list1Members[list1Members.Count - 1];

			using (var redis = new RedisGenericClient<T>())
			{
				list1Members.ForEach(x => redis.AddToList(ListId, x));
				list2Members.ForEach(x => redis.AddToList(ListId2, x));

				list1Members.Remove(lastItem);
				list2Members.Insert(0, lastItem);
				redis.PopAndPushBetweenLists(ListId, ListId2);

				var readList1 = redis.GetAllFromList(ListId);
				var readList2 = redis.GetAllFromList(ListId2);

				Factory.AssertListsAreEqual(readList1, list1Members);
				Factory.AssertListsAreEqual(readList2, list2Members);
			}
		}


		[Test]
		public void Can_enumerate_small_list()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				storeMembers.ForEach(x => redis.AddToList(ListId, x));

				var readMembers = new List<T>();
				foreach (var item in redis.Lists[ListId])
				{
					readMembers.Add(item);
				}
				Factory.AssertListsAreEqual(readMembers, storeMembers);
			}
		}

		[Test]
		public void Can_enumerate_large_list()
		{
			const int listSize = 2500;

			using (var redis = new RedisGenericClient<T>())
			{
				listSize.Times(x => redis.AddToList(ListId, Factory.CreateInstance(x)));

				var i = 0;
				foreach (var item in redis.Lists[ListId])
				{
					Factory.AssertIsEqual(item, Factory.CreateInstance(i++));
				}
			}
		}

		[Test]
		public void Can_Add_to_IList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				var members = list.ToList<T>();
				Factory.AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_Clear_IList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
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
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				Assert.That(list.Contains(Factory.ExistingValue), Is.True);
				Assert.That(list.Contains(Factory.NonExistingValue), Is.False);
			}
		}

		[Test]
		public void Can_Remove_value_from_IList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				storeMembers.Remove(Factory.ExistingValue);
				list.Remove(Factory.ExistingValue);

				var members = list.ToList<T>();

				Factory.AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_RemoveAt_value_from_IList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				storeMembers.RemoveAt(2);
				list.RemoveAt(2);

				var members = list.ToList<T>();

				Factory.AssertListsAreEqual(members, storeMembers);
			}
		}

		[Test]
		public void Can_get_default_index_from_IList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
			{
				var list = redis.Lists[ListId];
				storeMembers.ForEach(list.Add);

				for (var i=0; i < storeMembers.Count; i++)
				{
					Factory.AssertIsEqual(list[i], storeMembers[i]);
				}
			}
		}

		[Test]
		public void Can_test_for_IndexOf_in_IList()
		{
			var storeMembers = Factory.CreateList();
			using (var redis = new RedisGenericClient<T>())
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