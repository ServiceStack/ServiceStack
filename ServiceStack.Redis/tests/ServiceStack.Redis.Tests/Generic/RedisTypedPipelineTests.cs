using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture]
    public class RedisTypedPipelineTests
        : RedisClientTestsBase
    {
        public RedisTypedPipelineTests()
        {
            CleanMask = "gmultitest*";
        }

        private const string Key = "gmultitest";
        private const string ListKey = "gmultitest-list";
        private const string SetKey = "gmultitest-set";
        private const string SortedSetKey = "gmultitest-sortedset";

        readonly ShipperFactory modelFactory = new ShipperFactory();
        private IRedisTypedClient<Shipper> typedClient;
        private Shipper model;

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();

            typedClient = Redis.As<Shipper>();
            model = modelFactory.CreateInstance(1);
        }


        [Test]
        public void Can_call_single_operation_in_pipeline()
        {
            Assert.That(typedClient.GetValue(Key), Is.Null);

            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.SetValue(Key, model));

                pipeline.Flush();
            }

            modelFactory.AssertIsEqual(typedClient.GetValue(Key), model);
        }

        [Test]
        public void No_commit_of_atomic_pipelines_discards_all_commands()
        {
            Assert.That(typedClient.GetValue(Key), Is.Null);

            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.SetValue(Key, model));
            }

            Assert.That(typedClient.GetValue(Key), Is.Null);
        }

        [Test]
        public void Exception_in_atomic_pipelines_discards_all_commands()
        {
            Assert.That(typedClient.GetValue(Key), Is.Null);
            try
            {
                using (var pipeline = typedClient.CreatePipeline())
                {
                    pipeline.QueueCommand(r => r.SetValue(Key, model));
                    throw new NotSupportedException();
                }
            }
            catch (NotSupportedException)
            {
                Assert.That(typedClient.GetValue(Key), Is.Null);
            }
        }

        [Test]
        public void Can_call_single_operation_3_Times_in_pipeline()
        {
            var typedList = typedClient.Lists[ListKey];
            Assert.That(typedList.Count, Is.EqualTo(0));

            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(1)));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(2)));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(3)));

                pipeline.Flush();
            }

            Assert.That(typedList.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_call_single_operation_with_callback_3_Times_in_pipeline()
        {
            var results = new List<int>();

            var typedList = typedClient.Lists[ListKey];
            Assert.That(typedList.Count, Is.EqualTo(0));

            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(1)), () => results.Add(1));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(2)), () => results.Add(2));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(3)), () => results.Add(3));

                pipeline.Flush();
            }

            Assert.That(typedList.Count, Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public void Supports_different_operation_types_in_same_pipeline()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            var typedList = typedClient.Lists[ListKey];
            var typedSet = typedClient.Sets[SetKey];
            var typedSortedSet = typedClient.SortedSets[SortedSetKey];

            Assert.That(typedClient.GetValue(Key), Is.Null);
            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(1)));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(2)));
                pipeline.QueueCommand(r => r.AddItemToSet(typedSet, modelFactory.CreateInstance(3)));
                pipeline.QueueCommand(r => r.SetContainsItem(typedSet, modelFactory.CreateInstance(3)), b => containsItem = b);
                pipeline.QueueCommand(r => r.AddItemToSortedSet(typedSortedSet, modelFactory.CreateInstance(4)));
                pipeline.QueueCommand(r => r.AddItemToSortedSet(typedSortedSet, modelFactory.CreateInstance(5)));
                pipeline.QueueCommand(r => r.AddItemToSortedSet(typedSortedSet, modelFactory.CreateInstance(6)));
                pipeline.QueueCommand(r => r.GetListCount(typedList), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSetCount(typedSet), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSortedSetCount(typedSortedSet), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));

                pipeline.Flush();
            }

            Assert.That(containsItem, Is.True);
            Assert.That(Redis.GetValue(Key), Is.EqualTo("2"));
            Assert.That(incrementResults, Is.EquivalentTo(new List<int> { 1, 2 }));
            Assert.That(collectionCounts, Is.EquivalentTo(new List<int> { 2, 1, 3 }));

            modelFactory.AssertListsAreEqual(typedList.GetAll(), new List<Shipper>
                {
                    modelFactory.CreateInstance(1), modelFactory.CreateInstance(2)
                });

            Assert.That(typedSet.GetAll(), Is.EquivalentTo(new List<Shipper>
                   {
                       modelFactory.CreateInstance(3)
                   }));

            modelFactory.AssertListsAreEqual(typedSortedSet.GetAll(), new List<Shipper>
                {
                    modelFactory.CreateInstance(4), modelFactory.CreateInstance(5), modelFactory.CreateInstance(6)
                });
        }

        [Test]
        public void Can_call_multi_string_operations_in_pipeline()
        {
            Shipper item1 = null;
            Shipper item4 = null;

            var results = new List<Shipper>();

            var typedList = typedClient.Lists[ListKey];
            Assert.That(typedList.Count, Is.EqualTo(0));

            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(1)));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(2)));
                pipeline.QueueCommand(r => r.AddItemToList(typedList, modelFactory.CreateInstance(3)));
                pipeline.QueueCommand(r => r.GetAllItemsFromList(typedList), x => results = x);
                pipeline.QueueCommand(r => r.GetItemFromList(typedList, 0), x => item1 = x);
                pipeline.QueueCommand(r => r.GetItemFromList(typedList, 4), x => item4 = x);

                pipeline.Flush();
            }

            Assert.That(typedList.Count, Is.EqualTo(3));

            modelFactory.AssertListsAreEqual(results, new List<Shipper>
                {
                    modelFactory.CreateInstance(1), modelFactory.CreateInstance(2), modelFactory.CreateInstance(3)
                });

            modelFactory.AssertIsEqual(item1, modelFactory.CreateInstance(1));
            Assert.That(item4, Is.Null);
        }
        [Test]
        public void Pipeline_can_be_replayed()
        {
            const string keySquared = Key + Key;
            Assert.That(Redis.GetValue(Key), Is.Null);
            Assert.That(Redis.GetValue(keySquared), Is.Null);
            using (var pipeline = typedClient.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key));
                pipeline.QueueCommand(r => r.IncrementValue(keySquared));
                pipeline.Flush();

                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(keySquared), Is.EqualTo("1"));
                typedClient.RemoveEntry(Key);
                typedClient.RemoveEntry(keySquared);
                Assert.That(Redis.GetValue(Key), Is.Null);
                Assert.That(Redis.GetValue(keySquared), Is.Null);

                pipeline.Replay();
                pipeline.Dispose();
                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(keySquared), Is.EqualTo("1"));
            }

        }

    }
}