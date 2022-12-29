using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using ServiceStack.Text;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientListTestsAsync
        : RedisClientTestsBaseAsync
    {
        const string ListId = "rcl_testlist";
        const string ListId2 = "rcl_testlist2";
        private List<string> storeMembers;

        public RedisClientListTestsAsync()
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
        public async Task Can_PopAndPushItemBetweenLists()
        {
            await RedisAsync.AddItemToListAsync(ListId, "1");
            await RedisAsync.PopAndPushItemBetweenListsAsync(ListId, ListId2);
        }

        [Test]
        public async Task Can_BlockingPopAndPushItemBetweenLists()
        {
            await RedisAsync.AddItemToListAsync(ListId, "A");
            await RedisAsync.AddItemToListAsync(ListId, "B");
            var r = await RedisAsync.BlockingPopAndPushItemBetweenListsAsync(ListId, ListId2, new TimeSpan(0, 0, 1));

            Assert.That(r, Is.EqualTo("B"));
        }

        [Test]
        public async Task Can_Timeout_BlockingPopAndPushItemBetweenLists()
        {
            var r = await RedisAsync.BlockingPopAndPushItemBetweenListsAsync(ListId, ListId2, new TimeSpan(0, 0, 1));
            Assert.That(r, Is.Null);
        }

        [Test]
        public async Task Can_AddToList_and_GetAllFromList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var members = await RedisAsync.GetAllItemsFromListAsync(ListId);

            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_AddRangeToList_and_GetAllFromList()
        {
            await RedisAsync.AddRangeToListAsync(ListId, storeMembers);

            var members = await RedisAsync.GetAllItemsFromListAsync(ListId);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_PrependRangeToList_and_GetAllFromList()
        {
            await RedisAsync.PrependRangeToListAsync(ListId, storeMembers);

            var members = await RedisAsync.GetAllItemsFromListAsync(ListId);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_GetListCount()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var listCount = await RedisAsync.GetListCountAsync(ListId);

            Assert.That(listCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public async Task Can_GetItemFromList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var storeMember3 = storeMembers[2];
            var item3 = await RedisAsync.GetItemFromListAsync(ListId, 2);

            Assert.That(item3, Is.EqualTo(storeMember3));
        }

        [Test]
        public async Task Can_SetItemInList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            storeMembers[2] = "five";
            await RedisAsync.SetItemInListAsync(ListId, 2, "five");

            var members = await RedisAsync.GetAllItemsFromListAsync(ListId);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_PopFromList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var item4 = await RedisAsync.PopItemFromListAsync(ListId);

            Assert.That(item4, Is.EqualTo("four"));
        }

        [Test]
        public async Task Can_EnqueueOnList()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            foreach (var x in storeMembers)
            {
                await RedisAsync.EnqueueItemOnListAsync(ListId, x);
            }

            while (queue.Count > 0)
            {
                var actual = await RedisAsync.DequeueItemFromListAsync(ListId);
                Assert.That(actual, Is.EqualTo(queue.Dequeue()));
            }
        }

        [Test]
        public async Task Can_DequeueFromList()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            foreach (var x in storeMembers)
            {
                await RedisAsync.EnqueueItemOnListAsync(ListId, x);
            }

            var item1 = await RedisAsync.DequeueItemFromListAsync(ListId);

            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public async Task PopAndPushSameAsDequeue()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            foreach (var x in storeMembers)
            {
                await RedisAsync.EnqueueItemOnListAsync(ListId, x);
            }

            var item1 = await RedisAsync.PopAndPushItemBetweenListsAsync(ListId, ListId2);
            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public async Task Can_BlockingDequeueFromList()
        {
            var queue = new Queue<string>();
            storeMembers.ForEach(queue.Enqueue);
            foreach (var x in storeMembers)
            {
                await RedisAsync.EnqueueItemOnListAsync(ListId, x);
            }

            var item1 = await RedisAsync.BlockingDequeueItemFromListAsync(ListId, null);

            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public async Task BlockingDequeueFromList_Can_TimeOut()
        {
            var item1 = await RedisAsync.BlockingDequeueItemFromListAsync(ListId, TimeSpan.FromSeconds(1));
            Assert.That(item1, Is.Null);
        }

        [Test]
        public async Task Can_PushToList()
        {
            var stack = new Stack<string>();
            storeMembers.ForEach(stack.Push);
            foreach (var x in storeMembers)
            {
                await RedisAsync.PushItemToListAsync(ListId, x);
            }

            while (stack.Count > 0)
            {
                var actual = await RedisAsync.PopItemFromListAsync(ListId);
                Assert.That(actual, Is.EqualTo(stack.Pop()));
            }
        }

        [Test]
        public async Task Can_BlockingPopFromList()
        {
            var stack = new Stack<string>();
            storeMembers.ForEach(stack.Push);
            foreach (var x in storeMembers)
            {
                await RedisAsync.PushItemToListAsync(ListId, x);
            }

            var item1 = await RedisAsync.BlockingPopItemFromListAsync(ListId, null);

            Assert.That(item1, Is.EqualTo(stack.Pop()));
        }

        [Test]
        public async Task BlockingPopFromList_Can_TimeOut()
        {
            var item1 = await RedisAsync.BlockingPopItemFromListAsync(ListId, TimeSpan.FromSeconds(1));
            Assert.That(item1, Is.Null);
        }

        [Test]
        public async Task Can_RemoveStartFromList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var item1 = await RedisAsync.RemoveStartFromListAsync(ListId);

            Assert.That(item1, Is.EqualTo(storeMembers.First()));
        }

        [Test]
        public async Task Can_RemoveEndFromList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var item1 = await RedisAsync.RemoveEndFromListAsync(ListId);

            Assert.That(item1, Is.EqualTo(storeMembers.Last()));
        }

        [Test]
        public async Task Can_BlockingRemoveStartFromList()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var item1 = await RedisAsync.BlockingRemoveStartFromListAsync(ListId, null);

            Assert.That(item1, Is.EqualTo(storeMembers.First()));
        }

        [Test]
        public async Task Can_MoveBetweenLists()
        {
            var list1Members = new List<string> { "one", "two", "three", "four" };
            var list2Members = new List<string> { "five", "six", "seven" };
            const string item4 = "four";

            foreach (var x in list1Members)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }
            foreach (var x in list2Members)
            {
                await RedisAsync.AddItemToListAsync(ListId2, x);
            }

            list1Members.Remove(item4);
            list2Members.Insert(0, item4);
            await RedisAsync.PopAndPushItemBetweenListsAsync(ListId, ListId2);

            var readList1 = await RedisAsync.GetAllItemsFromListAsync(ListId);
            var readList2 = await RedisAsync.GetAllItemsFromListAsync(ListId2);

            AssertAreEqual(readList1, list1Members);
            AssertAreEqual(readList2, list2Members);
        }


        [Test]
        public async Task Can_enumerate_small_list()
        {
            foreach (var x in storeMembers)
            {
                await RedisAsync.AddItemToListAsync(ListId, x);
            }

            var readMembers = new List<string>();
            await foreach (var item in RedisAsync.Lists[ListId])
            {
                readMembers.Add(item);
            }
            AssertAreEqual(readMembers, storeMembers);
        }

        [Test]
        public async Task Can_enumerate_large_list()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int listSize = 2500;

            storeMembers = new List<string>();
            for (int x = 0; x < listSize; x++)
            {
                await RedisAsync.AddItemToListAsync(ListId, x.ToString());
                storeMembers.Add(x.ToString());
            }

            var members = new List<string>();
            await foreach (var item in RedisAsync.Lists[ListId])
            {
                members.Add(item);
            }
            members.Sort((x, y) => int.Parse(x).CompareTo(int.Parse(y)));
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_Add_to_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            var members = await ToListAsync(list);
            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_Clear_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            Assert.That(await list.CountAsync(), Is.EqualTo(storeMembers.Count));

            await list.ClearAsync();

            Assert.That(await list.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            Assert.That(await list.ContainsAsync("two"), Is.True);
            Assert.That(await list.ContainsAsync("five"), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            storeMembers.Remove("two");
            await list.RemoveAsync("two");

            var members = await ToListAsync(list);

            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_RemoveAt_value_from_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            storeMembers.RemoveAt(2);
            await list.RemoveAtAsync(2);

            var members = await ToListAsync(list);

            AssertAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_get_default_index_from_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            for (var i = 0; i < storeMembers.Count; i++)
            {
                Assert.That(await list.ElementAtAsync(i), Is.EqualTo(storeMembers[i]));
            }
        }

        [Test]
        public async Task Can_test_for_IndexOf_in_IList()
        {
            var list = RedisAsync.Lists[ListId];
            foreach (var x in storeMembers)
            {
                await list.AddAsync(x);
            }

            foreach (var item in storeMembers)
            {
                Assert.That(await list.IndexOfAsync(item), Is.EqualTo(storeMembers.IndexOf(item)));
            }
        }

        [Test]
        public async Task Can_AddRangeToList_and_GetSortedItems()
        {
            await RedisAsync.PrependRangeToListAsync(ListId, storeMembers);

            var members = await RedisAsync.GetSortedItemsFromListAsync(ListId, new SortOptions { SortAlpha = true, SortDesc = true, Skip = 1, Take = 2 });
            AssertAreEqual(members, storeMembers.OrderByDescending(s => s).Skip(1).Take(2).ToList());
        }

        public class Test
        {
            public string A { get; set; }
        }

        [Test]
        public async Task RemoveAll_removes_all_items_from_Named_List()
        {
            var redis = RedisAsync.As<Test>();

            var clientesRepo = redis.Lists["repo:Client:Test"];

            Assert.IsTrue(await clientesRepo.CountAsync() == 0, "Count 1 = " + await clientesRepo.CountAsync());
            await clientesRepo.AddAsync(new Test() { A = "Test" });
            Assert.IsTrue(await clientesRepo.CountAsync() == 1, "Count 2 = " + await clientesRepo.CountAsync());
            await clientesRepo.RemoveAllAsync();
            Assert.IsTrue(await clientesRepo.CountAsync() == 0, "Count 3 = " + await clientesRepo.CountAsync());
        }

    }

}