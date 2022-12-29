using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public class RedisTypedTransactionTestsAsync
        : RedisClientTestsBaseAsync
    {
        private const string Key = "multitest";
        private const string ListKey = "multitest-list";
        private const string SetKey = "multitest-set";
        private const string SortedSetKey = "multitest-sortedset";

        readonly ShipperFactory modelFactory = new ShipperFactory();
        private IRedisTypedClientAsync<Shipper> typedClient;
        private Shipper model;

        public RedisTypedTransactionTestsAsync()
        {
            CleanMask = "multitest*";
        }

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();

            typedClient = RedisAsync.As<Shipper>();
            model = modelFactory.CreateInstance(1);
        }

        [Test]
        public async Task Can_call_single_operation_in_transaction()
        {
            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);

            await using (var trans = await typedClient.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.SetValueAsync(Key, model));

                await trans.CommitAsync();
            }

            modelFactory.AssertIsEqual(await typedClient.GetValueAsync(Key), model);
        }

        [Test]
        public async Task No_commit_of_atomic_transactions_discards_all_commands()
        {
            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);

            await using (var trans = await typedClient.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.SetValueAsync(Key, model));
            }

            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
        }

        [Test]
        public async Task Exception_in_atomic_transactions_discards_all_commands()
        {
            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
            try
            {
                await using var trans = await typedClient.CreateTransactionAsync();
                trans.QueueCommand(r => r.SetValueAsync(Key, model));
                throw new NotSupportedException();
            }
            catch (NotSupportedException)
            {
                Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
            }
        }

        [Test]
        public async Task Can_call_single_operation_3_Times_in_transaction()
        {
            var typedList = typedClient.Lists[ListKey];
            Assert.That(await typedList.CountAsync(), Is.EqualTo(0));

            await using (var trans = await typedClient.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(3)));

                await trans.CommitAsync();
            }

            Assert.That(await typedList.CountAsync(), Is.EqualTo(3));
        }

        [Test]
        public async Task Can_call_single_operation_with_callback_3_Times_in_transaction()
        {
            var results = new List<int>();

            var typedList = typedClient.Lists[ListKey];
            Assert.That(await typedList.CountAsync(), Is.EqualTo(0));

            await using (var trans = await typedClient.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)), () => results.Add(1));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)), () => results.Add(2));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(3)), () => results.Add(3));

                await trans.CommitAsync();
            }

            Assert.That(await typedList.CountAsync(), Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public async Task Supports_different_operation_types_in_same_transaction()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            var typedList = typedClient.Lists[ListKey];
            var typedSet = typedClient.Sets[SetKey];
            var typedSortedSet = typedClient.SortedSets[SortedSetKey];

            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
            await using (var trans = await typedClient.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)));
                trans.QueueCommand(r => r.AddItemToSetAsync(typedSet, modelFactory.CreateInstance(3)));
                trans.QueueCommand(r => r.SetContainsItemAsync(typedSet, modelFactory.CreateInstance(3)), b => containsItem = b);
                trans.QueueCommand(r => r.AddItemToSortedSetAsync(typedSortedSet, modelFactory.CreateInstance(4)));
                trans.QueueCommand(r => r.AddItemToSortedSetAsync(typedSortedSet, modelFactory.CreateInstance(5)));
                trans.QueueCommand(r => r.AddItemToSortedSetAsync(typedSortedSet, modelFactory.CreateInstance(6)));
                trans.QueueCommand(r => r.GetListCountAsync(typedList), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.GetSetCountAsync(typedSet), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.GetSortedSetCountAsync(typedSortedSet), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));

                await trans.CommitAsync();
            }

            Assert.That(containsItem, Is.True);
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("2"));
            Assert.That(incrementResults, Is.EquivalentTo(new List<int> { 1, 2 }));
            Assert.That(collectionCounts, Is.EquivalentTo(new List<int> { 2, 1, 3 }));

            modelFactory.AssertListsAreEqual(await typedList.GetAllAsync(), new List<Shipper>
                {
                    modelFactory.CreateInstance(1), modelFactory.CreateInstance(2)
                });

            Assert.That(await typedSet.GetAllAsync(), Is.EquivalentTo(new List<Shipper>
                   {
                       modelFactory.CreateInstance(3)
                   }));

            modelFactory.AssertListsAreEqual(await typedSortedSet.GetAllAsync(), new List<Shipper>
                {
                    modelFactory.CreateInstance(4), modelFactory.CreateInstance(5), modelFactory.CreateInstance(6)
                });
        }

        [Test]
        public async Task Can_call_multi_string_operations_in_transaction()
        {
            Shipper item1 = null;
            Shipper item4 = null;

            var results = new List<Shipper>();

            var typedList = typedClient.Lists[ListKey];
            Assert.That(await typedList.CountAsync(), Is.EqualTo(0));

            await using (var trans = await typedClient.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)));
                trans.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(3)));
                trans.QueueCommand(r => r.GetAllItemsFromListAsync(typedList), x => results = x);
                trans.QueueCommand(r => r.GetItemFromListAsync(typedList, 0), x => item1 = x);
                trans.QueueCommand(r => r.GetItemFromListAsync(typedList, 4), x => item4 = x);

                await trans.CommitAsync();
            }

            Assert.That(await typedList.CountAsync(), Is.EqualTo(3));

            modelFactory.AssertListsAreEqual(results, new List<Shipper>
                {
                    modelFactory.CreateInstance(1), modelFactory.CreateInstance(2), modelFactory.CreateInstance(3)
                });

            modelFactory.AssertIsEqual(item1, modelFactory.CreateInstance(1));
            Assert.That(item4, Is.Null);
        }
        [Test]
        // Operations that are not supported in older versions will look at server info to determine what to do.
        // If server info is fetched each time, then it will interfer with transaction
        public async Task Can_call_operation_not_supported_on_older_servers_in_transaction()
        {
            var temp = new byte[1];
            await using var trans = await RedisAsync.CreateTransactionAsync();
            trans.QueueCommand(r => ((IRedisNativeClientAsync)r).SetExAsync("key", 5, temp));
            await trans.CommitAsync();
        }


        [Test]
        public async Task Transaction_can_be_replayed()
        {
            string KeySquared = Key + Key;
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.Null);
            await using var trans = await RedisAsync.CreateTransactionAsync();
            trans.QueueCommand(r => r.IncrementValueAsync(Key));
            trans.QueueCommand(r => r.IncrementValueAsync(KeySquared));
            await trans.CommitAsync();

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.EqualTo("1"));
            await NativeAsync.DelAsync(Key);
            await NativeAsync.DelAsync(KeySquared);
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.Null);

            await trans.ReplayAsync();
            await trans.DisposeAsync();
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.EqualTo("1"));
        }

    }
}