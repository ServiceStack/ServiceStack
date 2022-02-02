using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture, Category("Async")]
    public abstract class RedisClientListTestsBaseAsync<T>
    {
        const string ListId = "testlist";
        const string ListId2 = "testlist2";
        private IRedisListAsync<T> List;
        private IRedisListAsync<T> List2;

        protected abstract IModelFactory<T> Factory { get; }

        private IRedisClientAsync client;
        private IRedisTypedClientAsync<T> redis;

        [SetUp]
        public async Task SetUp()
        {
            if (client is object)
            {
                await client.DisposeAsync();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost);
            await client.FlushAllAsync();

            redis = client.As<T>();

            List = redis.Lists[ListId];
            List2 = redis.Lists[ListId2];
        }

        [Test]
        public async Task Can_AddToList_and_GetAllFromList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            var members = await redis.GetAllItemsFromListAsync(List);

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_GetListCount()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            var listCount = await redis.GetListCountAsync(List);

            Assert.That(listCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public async Task Can_GetItemFromList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            var storeMember3 = storeMembers[2];
            var item3 = await redis.GetItemFromListAsync(List, 2);

            Factory.AssertIsEqual(item3, storeMember3);
        }

        [Test]
        public async Task Can_SetItemInList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            storeMembers[2] = Factory.NonExistingValue;
            await redis.SetItemInListAsync(List, 2, Factory.NonExistingValue);

            var members = await redis.GetAllItemsFromListAsync(List);

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_PopFromList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            var lastValue = await redis.PopItemFromListAsync(List);

            Factory.AssertIsEqual(lastValue, storeMembers[storeMembers.Count - 1]);
        }

        [Test]
        public async Task Can_BlockingDequeueItemFromList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.EnqueueItemOnListAsync(List, x));

            var item1 = await redis.BlockingDequeueItemFromListAsync(List, new TimeSpan(0, 0, 1));

            Factory.AssertIsEqual(item1, (T)storeMembers.First());
        }

        [Test]
        public async Task Can_BlockingDequeueItemFromList_Timeout()
        {
            var item1 = await redis.BlockingDequeueItemFromListAsync(List, new TimeSpan(0, 0, 1));
            Assert.AreEqual(item1, default(T));
        }

        [Test]
        public async Task Can_DequeueFromList()
        {

            var queue = new Queue<T>();
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => queue.Enqueue(x));
            await storeMembers.ForEachAsync(x => redis.EnqueueItemOnListAsync(List, x));

            var item1 = await redis.DequeueItemFromListAsync(List);

            Factory.AssertIsEqual(item1, queue.Dequeue());
        }

        [Test]
        public async Task PopAndPushSameAsDequeue()
        {
            var queue = new Queue<T>();
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => queue.Enqueue(x));
            await storeMembers.ForEachAsync(x => redis.EnqueueItemOnListAsync(List, x));

            var item1 = await redis.PopAndPushItemBetweenListsAsync(List, List2);
            Assert.That(item1, Is.EqualTo(queue.Dequeue()));
        }

        [Test]
        public async Task Can_ClearList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.EnqueueItemOnListAsync(List, x));

            var count = (await redis.GetAllItemsFromListAsync(List)).Count;
            Assert.That(count, Is.EqualTo(storeMembers.Count));

            await redis.RemoveAllFromListAsync(List);
            count = (await redis.GetAllItemsFromListAsync(List)).Count;
            Assert.That(count, Is.EqualTo(0));

        }

        [Test]
        public async Task Can_ClearListWithOneItem()
        {
            var storeMembers = Factory.CreateList();
            await redis.EnqueueItemOnListAsync(List, storeMembers[0]);

            var count = (await redis.GetAllItemsFromListAsync(List)).Count;
            Assert.That(count, Is.EqualTo(1));

            await redis.RemoveAllFromListAsync(List);
            count = (await redis.GetAllItemsFromListAsync(List)).Count;
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task Can_MoveBetweenLists()
        {
            var list1Members = Factory.CreateList();
            var list2Members = Factory.CreateList2();
            var lastItem = list1Members[list1Members.Count - 1];

            await list1Members.ForEachAsync(x => redis.AddItemToListAsync(List, x));
            await list2Members.ForEachAsync(x => redis.AddItemToListAsync(List2, x));

            list1Members.Remove(lastItem);
            list2Members.Insert(0, lastItem);
            await redis.PopAndPushItemBetweenListsAsync(List, List2);

            var readList1 = await redis.GetAllItemsFromListAsync(List);
            var readList2 = await redis.GetAllItemsFromListAsync(List2);

            Factory.AssertListsAreEqual(readList1, list1Members);
            Factory.AssertListsAreEqual(readList2, list2Members);
        }


        [Test]
        public async Task Can_enumerate_small_list()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            var readMembers = new List<T>();
            await foreach (var item in redis.Lists[ListId])
            {
                readMembers.Add(item);
            }
            Factory.AssertListsAreEqual(readMembers, storeMembers);
        }

        [Test]
        public async Task Can_enumerate_large_list()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int listSize = 2500;

            await listSize.TimesAsync(x => redis.AddItemToListAsync(List, Factory.CreateInstance(x)));

            var i = 0;
            await foreach (var item in List)
            {
                Factory.AssertIsEqual(item, Factory.CreateInstance(i++));
            }
        }

        [Test]
        public async Task Can_Add_to_IList()
        {
            var storeMembers = Factory.CreateList();
            var list = redis.Lists[ListId];
            await storeMembers.ForEachAsync(x => list.AddAsync(x));

            var members = await list.ToListAsync<T>();
            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_Clear_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            Assert.That(await List.CountAsync(), Is.EqualTo(storeMembers.Count));

            await List.ClearAsync();

            Assert.That(await List.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            Assert.That(await List.ContainsAsync(Factory.ExistingValue), Is.True);
            Assert.That(await List.ContainsAsync(Factory.NonExistingValue), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            storeMembers.Remove(Factory.ExistingValue);
            await List.RemoveAsync(Factory.ExistingValue);

            var members = await List.ToListAsync<T>();

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_RemoveAt_value_from_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            storeMembers.RemoveAt(2);
            await List.RemoveAtAsync(2);

            var members = await List.ToListAsync<T>();

            Factory.AssertListsAreEqual(members, storeMembers);
        }

        [Test]
        public async Task Can_get_default_index_from_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            for (var i = 0; i < storeMembers.Count; i++)
            {
                Factory.AssertIsEqual(await List.ElementAtAsync(i), storeMembers[i]);
            }
        }

        [Test]
        public async Task Can_test_for_IndexOf_in_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            foreach (var item in storeMembers)
            {
                Assert.That(await List.IndexOfAsync(item), Is.EqualTo(storeMembers.IndexOf(item)));
            }
        }


        [Test]
        public async Task Can_GetRangeFromList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => redis.AddItemToListAsync(List, x));

            //in SetUp(): List = redis.Lists["testlist"];
            //alias for: redis.GetRangeFromList(redis.Lists["testlist"], 1, 3);
            var range = await List.GetRangeAsync(1, 3);
            var expected = storeMembers.Skip(1).Take(3).ToList();

            //Uncomment to view list contents
            //Debug.WriteLine(range.Dump());
            //Debug.WriteLine(expected.Dump());

            Factory.AssertListsAreEqual(range, expected);
        }

    }
}