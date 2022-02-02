using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;
using ServiceStack.Redis.Tests.Support;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public abstract class RedisClientListTestsBase<T>
    {
        const string ListId = "testlist";
        const string ListId2 = "testlist2";
        private IRedisList<T> List;
        private IRedisList<T> List2;

        protected abstract IModelFactory<T> Factory { get; }

        private RedisClient client;
        private IRedisTypedClient<T> redis;

        [SetUp]
        public void SetUp()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost);
            client.FlushAll();

            redis = client.As<T>();

            List = redis.Lists[ListId];
            List2 = redis.Lists[ListId2];
        }

        [Test]
        public void Can_AddToList_and_GetAllFromList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            var members = redis.GetAllItemsFromList(List);

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_GetListCount()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            var listCount = redis.GetListCount(List);

            Assert.That(listCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public void Can_GetItemFromList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            var storeMember3 = storeMembers[2];
            var item3 = redis.GetItemFromList(List, 2);

            Factory.AssertIsEqual(item3, storeMember3);
        }

        [Test]
        public void Can_SetItemInList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            storeMembers[2] = Factory.NonExistingValue;
            redis.SetItemInList(List, 2, Factory.NonExistingValue);

            var members = redis.GetAllItemsFromList(List);

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_PopFromList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            var lastValue = redis.PopItemFromList(List);

            Factory.AssertIsEqual(lastValue, storeMembers[storeMembers.Count - 1]);
        }

        [Test]
        public void Can_BlockingDequeueItemFromList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.EnqueueItemOnList(List, x));

            var item1 = redis.BlockingDequeueItemFromList(List, new TimeSpan(0, 0, 1));

            Factory.AssertIsEqual(item1, (T)storeMembers.First());
        }

        [Test]
        public void Can_BlockingDequeueItemFromList_Timeout()
        {
            var item1 = redis.BlockingDequeueItemFromList(List, new TimeSpan(0, 0, 1));
            Assert.AreEqual(item1, default(T));
        }

        [Test]
        public void Can_DequeueFromList()
        {

            var queue = new Queue<T>();
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => queue.Enqueue(x));
            storeMembers.ForEach(x => redis.EnqueueItemOnList(List, x));

            var item1 = redis.DequeueItemFromList(List);

            Factory.AssertIsEqual(item1, queue.Dequeue());
        }

        [Test]
        public void PopAndPushSameAsDequeue()
        {
            var queue = new Queue<T>();
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => queue.Enqueue(x));
            storeMembers.ForEach(x => redis.EnqueueItemOnList(List, x));

            var item1 = redis.PopAndPushItemBetweenLists(List, List2);
            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public void Can_ClearList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.EnqueueItemOnList(List, x));

            var count = redis.GetAllItemsFromList(List).Count;
            Assert.That(count, Is.EqualTo(storeMembers.Count));

            redis.RemoveAllFromList(List);
            count = redis.GetAllItemsFromList(List).Count;
            Assert.That(count, Is.EqualTo(0));

        }

        [Test]
        public void Can_ClearListWithOneItem()
        {
            var storeMembers = Factory.CreateList();
            redis.EnqueueItemOnList(List, storeMembers[0]);

            var count = redis.GetAllItemsFromList(List).Count;
            Assert.That(count, Is.EqualTo(1));

            redis.RemoveAllFromList(List);
            count = redis.GetAllItemsFromList(List).Count;
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void Can_MoveBetweenLists()
        {
            var list1Members = Factory.CreateList();
            var list2Members = Factory.CreateList2();
            var lastItem = list1Members[list1Members.Count - 1];

            list1Members.ForEach(x => redis.AddItemToList(List, x));
            list2Members.ForEach(x => redis.AddItemToList(List2, x));

            list1Members.Remove(lastItem);
            list2Members.Insert(0, lastItem);
            redis.PopAndPushItemBetweenLists(List, List2);

            var readList1 = redis.GetAllItemsFromList(List);
            var readList2 = redis.GetAllItemsFromList(List2);

            Factory.AssertListsAreEqual(readList1, list1Members);
            Factory.AssertListsAreEqual(readList2, list2Members);
        }


        [Test]
        public void Can_enumerate_small_list()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            var readMembers = new List<T>();
            foreach (var item in redis.Lists[ListId])
            {
                readMembers.Add(item);
            }
            Factory.AssertListsAreEqual(readMembers, storeMembers);
        }

        [Test]
        public void Can_enumerate_large_list()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int listSize = 2500;

            listSize.Times(x => redis.AddItemToList(List, Factory.CreateInstance(x)));

            var i = 0;
            foreach (var item in List)
            {
                Factory.AssertIsEqual(item, Factory.CreateInstance(i++));
            }
        }

        [Test]
        public void Can_Add_to_IList()
        {
            var storeMembers = Factory.CreateList();
            var list = redis.Lists[ListId];
            storeMembers.ForEach(list.Add);

            var members = list.ToList<T>();
            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_Clear_IList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(List.Add);

            Assert.That(List.Count, Is.EqualTo(storeMembers.Count));

            List.Clear();

            Assert.That(List.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Test_Contains_in_IList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(List.Add);

            Assert.That(List.Contains(Factory.ExistingValue), Is.True);
            Assert.That(List.Contains(Factory.NonExistingValue), Is.False);
        }

        [Test]
        public void Can_Remove_value_from_IList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(List.Add);

            storeMembers.Remove(Factory.ExistingValue);
            List.Remove(Factory.ExistingValue);

            var members = List.ToList<T>();

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_RemoveAt_value_from_IList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(List.Add);

            storeMembers.RemoveAt(2);
            List.RemoveAt(2);

            var members = List.ToList<T>();

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_get_default_index_from_IList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(List.Add);

            for (var i = 0; i < storeMembers.Count; i++)
            {
                Factory.AssertIsEqual(List[i], storeMembers[i]);
            }
        }

        [Test]
        public void Can_test_for_IndexOf_in_IList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(List.Add);

            foreach (var item in storeMembers)
            {
                Assert.That(List.IndexOf(item), Is.EqualTo(storeMembers.IndexOf(item)));
            }
        }


        [Test]
        public void Can_GetRangeFromList()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToList(List, x));

            //in SetUp(): List = redis.Lists["testlist"];
            //alias for: redis.GetRangeFromList(redis.Lists["testlist"], 1, 3);
            var range = List.GetRange(1, 3);
            var expected = storeMembers.Skip(1).Take(3).ToList();

            //Uncomment to view list contents
            //Debug.WriteLine(range.Dump());
            //Debug.WriteLine(expected.Dump());

            Factory.AssertListsAreEqual(range, expected);
        }

    }
}