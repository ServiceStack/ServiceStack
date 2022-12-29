using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientListTests
        : RedisClientTestsBase
    {
        const string ListId = "rcl_testlist";
        const string ListId2 = "rcl_testlist2";
        private List<string> storeMembers;

        public RedisClientListTests()
        {
            CleanMask = "rcl_testlist*";
        }

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            storeMembers = new List<string> { "one", "two", "three", "four" };
        }

        private static void AssertAreEqual(List<string> actualList, List<string> expectedList)
        {
            Assert.That(actualList, Has.Count.EqualTo(expectedList.Count));
            var i = 0;
            actualList.ForEach(x => Assert.That(x, Is.EqualTo(expectedList[i++])));
        }

        private static void AssertAreEqual(List<string> actualList, Queue<string> expectedList)
        {
            Assert.That(actualList, Has.Count.EqualTo(expectedList.Count));
            actualList.ForEach(x => Assert.That(x, Is.EqualTo(expectedList.Dequeue())));
        }

        [Test]
        public void Can_PopAndPushItemBetweenLists()
        {
            Redis.AddItemToList(ListId, "1");
            Redis.PopAndPushItemBetweenLists(ListId, ListId2);
        }

        [Test]
        public void Can_BlockingPopAndPushItemBetweenLists()
        {
            Redis.AddItemToList(ListId, "A");
            Redis.AddItemToList(ListId, "B");
            var r = Redis.BlockingPopAndPushItemBetweenLists(ListId, ListId2, new TimeSpan(0, 0, 1));

            Assert.That(r, Is.EqualTo("B"));
        }

        [Test]
        public void Can_Timeout_BlockingPopAndPushItemBetweenLists()
        {
            var r = Redis.BlockingPopAndPushItemBetweenLists(ListId, ListId2, new TimeSpan(0, 0, 1));
            Assert.That(r, Is.Null);
        }

        [Test]
        public void Can_AddToList_and_GetAllFromList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var members = Redis.GetAllItemsFromList(ListId);

            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_AddRangeToList_and_GetAllFromList()
        {
            Redis.AddRangeToList(ListId, storeMembers);

            var members = Redis.GetAllItemsFromList(ListId);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_PrependRangeToList_and_GetAllFromList()
        {
            Redis.PrependRangeToList(ListId, storeMembers);

            var members = Redis.GetAllItemsFromList(ListId);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_GetListCount()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var listCount = Redis.GetListCount(ListId);

            Assert.That(listCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public void Can_GetItemFromList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var storeMember3 = storeMembers[2];
            var item3 = Redis.GetItemFromList(ListId, 2);

            Assert.That(item3, Is.EqualTo(storeMember3));
        }

        [Test]
        public void Can_SetItemInList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            storeMembers[2] = "five";
            Redis.SetItemInList(ListId, 2, "five");

            var members = Redis.GetAllItemsFromList(ListId);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public void Can_PopFromList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var item4 = Redis.PopItemFromList(ListId);

            Assert.That(item4, Is.EqualTo("four"));
        }

        [Test]
        public void Can_EnqueueOnList()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            storeMembers.ForEach(x => Redis.EnqueueItemOnList(ListId, x));

            while (queue.Count > 0)
            {
                var actual = Redis.DequeueItemFromList(ListId);
                Assert.That(actual, Is.EqualTo(queue.Dequeue()));
            }
        }

        [Test]
        public void Can_DequeueFromList()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            storeMembers.ForEach(x => Redis.EnqueueItemOnList(ListId, x));

            var item1 = Redis.DequeueItemFromList(ListId);

            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public void PopAndPushSameAsDequeue()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            storeMembers.ForEach(x => Redis.EnqueueItemOnList(ListId, x));

            var item1 = Redis.PopAndPushItemBetweenLists(ListId, ListId2);
            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public void Can_BlockingDequeueFromList()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            storeMembers.ForEach(x => Redis.EnqueueItemOnList(ListId, x));

            var item1 = Redis.BlockingDequeueItemFromList(ListId, null);

            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public void BlockingDequeueFromList_Can_TimeOut()
        {
            var item1 = Redis.BlockingDequeueItemFromList(ListId, TimeSpan.FromSeconds(1));
            Assert.That(item1, Is.Null);
        }

        [Test]
        public void Can_PushToList()
        {
            var stack = new Stack<string>();
            storeMembers.ForEach(stack.Push);
            storeMembers.ForEach(x => Redis.PushItemToList(ListId, x));

            while (stack.Count > 0)
            {
                var actual = Redis.PopItemFromList(ListId);
                Assert.That(actual, Is.EqualTo(stack.Pop()));
            }
        }

        [Test]
        public void Can_BlockingPopFromList()
        {
            var stack = new Stack<string>();
            storeMembers.ForEach(stack.Push);
            storeMembers.ForEach(x => Redis.PushItemToList(ListId, x));

            var item1 = Redis.BlockingPopItemFromList(ListId, null);

            Assert.That(item1, Is.EqualTo(stack.Pop()));
        }

        [Test]
        public void BlockingPopFromList_Can_TimeOut()
        {
            var item1 = Redis.BlockingPopItemFromList(ListId, TimeSpan.FromSeconds(1));
            Assert.That(item1, Is.Null);
        }

        [Test]
        public void Can_RemoveStartFromList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var item1 = Redis.RemoveStartFromList(ListId);

            Assert.That(item1, Is.EqualTo(storeMembers.First()));
        }

        [Test]
        public void Can_RemoveEndFromList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var item1 = Redis.RemoveEndFromList(ListId);

            Assert.That(item1, Is.EqualTo(storeMembers.Last()));
        }

        [Test]
        public void Can_BlockingRemoveStartFromList()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

            var item1 = Redis.BlockingRemoveStartFromList(ListId, null);

            Assert.That(item1, Is.EqualTo(storeMembers.First()));
        }

        [Test]
        public void Can_MoveBetweenLists()
        {
            var list1Members = new List<string> { "one", "two", "three", "four" };
            var list2Members = new List<string> { "five", "six", "seven" };
            const string item4 = "four";

            list1Members.ForEach(x => Redis.AddItemToList(ListId, x));
            list2Members.ForEach(x => Redis.AddItemToList(ListId2, x));

            list1Members.Remove(item4);
            list2Members.Insert(0, item4);
            Redis.PopAndPushItemBetweenLists(ListId, ListId2);

            var readList1 = Redis.GetAllItemsFromList(ListId);
            var readList2 = Redis.GetAllItemsFromList(ListId2);

            AssertAreEqual(readList1, list1Members);
            AssertAreEqual(readList2, list2Members);
        }


        [Test]
        public void Can_enumerate_small_list()
        {
            storeMembers.ForEach(x => Redis.AddItemToList(ListId, x));

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
                Redis.AddItemToList(ListId, x.ToString());
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

        [Test]
        public void Can_AddRangeToList_and_GetSortedItems()
        {
            Redis.PrependRangeToList(ListId, storeMembers);

            var members = Redis.GetSortedItemsFromList(ListId, new SortOptions { SortAlpha = true, SortDesc = true, Skip = 1, Take = 2 });
            AssertAreEqual(members, storeMembers.OrderByDescending(s => s).Skip(1).Take(2).ToList());
        }

        public class Test
        {
            public string A { get; set; }
        }

        [Test]
        public void RemoveAll_removes_all_items_from_Named_List()
        {
            var redis = Redis.As<Test>();

            var clientesRepo = redis.Lists["repo:Client:Test"];

            Assert.IsTrue(clientesRepo.Count == 0, "Count 1 = " + clientesRepo.Count);
            clientesRepo.Add(new Test() { A = "Test" });
            Assert.IsTrue(clientesRepo.Count == 1, "Count 2 = " + clientesRepo.Count);
            clientesRepo.RemoveAll();
            Assert.IsTrue(clientesRepo.Count == 0, "Count 3 = " + clientesRepo.Count);
        }

    }

}