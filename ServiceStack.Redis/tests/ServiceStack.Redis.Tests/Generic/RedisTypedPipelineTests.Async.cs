using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public class RedisTypedPipelineTestsAsync
        : RedisClientTestsBaseAsync
    {
        public RedisTypedPipelineTestsAsync()
        {
            CleanMask = "gmultitest*";
        }

        private const string Key = "gmultitest";
        private const string ListKey = "gmultitest-list";
        private const string SetKey = "gmultitest-set";
        private const string SortedSetKey = "gmultitest-sortedset";

        readonly ShipperFactory modelFactory = new ShipperFactory();
        private IRedisTypedClientAsync<Shipper> typedClient;
        private Shipper model;

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();

            typedClient = RedisAsync.As<Shipper>();
            model = modelFactory.CreateInstance(1);
        }


        [Test]
        public async Task Can_call_single_operation_in_pipeline()
        {
            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);

            await using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.SetValueAsync(Key, model));

                await pipeline.FlushAsync();
            }

            modelFactory.AssertIsEqual(await typedClient.GetValueAsync(Key), model);
        }

        [Test]
        public async Task No_commit_of_atomic_pipelines_discards_all_commands()
        {
            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);

            await using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.SetValueAsync(Key, model));
            }

            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
        }

        [Test]
        public async Task Exception_in_atomic_pipelines_discards_all_commands()
        {
            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
            try
            {
                await using var pipeline = typedClient.CreatePipeline();
                pipeline.QueueCommand(r => r.SetValueAsync(Key, model));
                throw new NotSupportedException();
            }
            catch (NotSupportedException)
            {
                Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
            }
        }

        [Test]
        public async Task Can_call_single_operation_3_Times_in_pipeline()
        {
            var typedList = typedClient.Lists[ListKey];
            Assert.That(await typedList.CountAsync(), Is.EqualTo(0));

            await using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(3)));

                await pipeline.FlushAsync();
            }

            Assert.That(await typedList.CountAsync(), Is.EqualTo(3));
        }

        [Test]
        public async Task Can_call_single_operation_with_callback_3_Times_in_pipeline()
        {
            var results = new List<int>();

            var typedList = typedClient.Lists[ListKey];
            Assert.That(await typedList.CountAsync(), Is.EqualTo(0));

            await using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)), () => results.Add(1));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)), () => results.Add(2));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(3)), () => results.Add(3));

                await pipeline.FlushAsync();
            }

            Assert.That(await typedList.CountAsync(), Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public async Task Supports_different_operation_types_in_same_pipeline()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            var typedList = typedClient.Lists[ListKey];
            var typedSet = typedClient.Sets[SetKey];
            var typedSortedSet = typedClient.SortedSets[SortedSetKey];

            Assert.That(await typedClient.GetValueAsync(Key), Is.Null);
            await using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)));
                pipeline.QueueCommand(r => r.AddItemToSetAsync(typedSet, modelFactory.CreateInstance(3)));
                pipeline.QueueCommand(r => r.SetContainsItemAsync(typedSet, modelFactory.CreateInstance(3)), b => containsItem = b);
                pipeline.QueueCommand(r => r.AddItemToSortedSetAsync(typedSortedSet, modelFactory.CreateInstance(4)));
                pipeline.QueueCommand(r => r.AddItemToSortedSetAsync(typedSortedSet, modelFactory.CreateInstance(5)));
                pipeline.QueueCommand(r => r.AddItemToSortedSetAsync(typedSortedSet, modelFactory.CreateInstance(6)));
                pipeline.QueueCommand(r => r.GetListCountAsync(typedList), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSetCountAsync(typedSet), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSortedSetCountAsync(typedSortedSet), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));

                await pipeline.FlushAsync();
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
        public async Task Can_call_multi_string_operations_in_pipeline()
        {
            Shipper item1 = null;
            Shipper item4 = null;

            var results = new List<Shipper>();

            var typedList = typedClient.Lists[ListKey];
            Assert.That(await typedList.CountAsync(), Is.EqualTo(0));

            await using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(1)));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(2)));
                pipeline.QueueCommand(r => r.AddItemToListAsync(typedList, modelFactory.CreateInstance(3)));
                pipeline.QueueCommand(r => r.GetAllItemsFromListAsync(typedList), x => results = x);
                pipeline.QueueCommand(r => r.GetItemFromListAsync(typedList, 0), x => item1 = x);
                pipeline.QueueCommand(r => r.GetItemFromListAsync(typedList, 4), x => item4 = x);

                await pipeline.FlushAsync();
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
        public async Task Pipeline_can_be_replayed()
        {
            const string keySquared = Key + Key;
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(keySquared), Is.Null);
            await using var pipeline = typedClient.CreatePipeline();
            pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
            pipeline.QueueCommand(r => r.IncrementValueAsync(keySquared));
            await pipeline.FlushAsync();

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(keySquared), Is.EqualTo("1"));
            await typedClient.RemoveEntryAsync(Key);
            await typedClient.RemoveEntryAsync(keySquared);
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(keySquared), Is.Null);

            await pipeline.ReplayAsync();
            await pipeline.DisposeAsync();
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(keySquared), Is.EqualTo("1"));

        }

    }
}