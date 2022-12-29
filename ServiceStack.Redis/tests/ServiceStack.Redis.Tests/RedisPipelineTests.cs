using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisPipelineTests
        : RedisClientTestsBase
    {
        private const string Key = "pipemultitest";
        private const string ListKey = "pipemultitest-list";
        private const string SetKey = "pipemultitest-set";
        private const string SortedSetKey = "pipemultitest-sortedset";

        public override void OnAfterEachTest()
        {
            CleanMask = Key + "*";
            base.OnAfterEachTest();
        }

        [Test]
        public void Can_call_single_operation_in_pipeline()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key));
                var map = new Dictionary<string, int>();
                pipeline.QueueCommand(r => r.Get<int>(Key), y => map[Key] = y);

                pipeline.Flush();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
        }

        [Test]
        public void No_commit_of_atomic_pipelines_discards_all_commands()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key));
            }
            Assert.That(Redis.GetValue(Key), Is.Null);
        }

        [Test]
        public void Exception_in_atomic_pipelines_discards_all_commands()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            try
            {
                using (var pipeline = Redis.CreatePipeline())
                {
                    pipeline.QueueCommand(r => r.IncrementValue(Key));
                    throw new NotSupportedException();
                }
            }
            catch (NotSupportedException)
            {
                Assert.That(Redis.GetValue(Key), Is.Null);
            }
        }

        [Test]
        public void Can_call_single_operation_3_Times_in_pipeline()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key));
                pipeline.QueueCommand(r => r.IncrementValue(Key));
                pipeline.QueueCommand(r => r.IncrementValue(Key));

                pipeline.Flush();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("3"));
        }
        [Test]
        public void Can_call_hash_operations_in_pipeline()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            var fields = new[] { "field1", "field2", "field3" };
            var values = new[] { "1", "2", "3" };
            var fieldBytes = new byte[fields.Length][];
            for (int i = 0; i < fields.Length; ++i)
            {
                fieldBytes[i] = GetBytes(fields[i]);

            }
            var valueBytes = new byte[values.Length][];
            for (int i = 0; i < values.Length; ++i)
            {
                valueBytes[i] = GetBytes(values[i]);

            }
            byte[][] members = null;
            var pipeline = Redis.CreatePipeline();


            pipeline.QueueCommand(r => ((RedisNativeClient)r).HMSet(Key, fieldBytes, valueBytes));
            pipeline.QueueCommand(r => ((RedisNativeClient)r).HGetAll(Key), x => members = x);


            pipeline.Flush();


            for (var i = 0; i < members.Length; i += 2)
            {
                Assert.AreEqual(members[i], fieldBytes[i / 2]);
                Assert.AreEqual(members[i + 1], valueBytes[i / 2]);

            }
            pipeline.Dispose();
        }

        [Test]
        public void Can_call_multiple_setexs_in_pipeline()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            var keys = new[] { Key + "key1", Key + "key2", Key + "key3" };
            var values = new[] { "1", "2", "3" };
            var pipeline = Redis.CreatePipeline();

            for (int i = 0; i < 3; ++i)
            {
                int index0 = i;
                pipeline.QueueCommand(r => ((RedisNativeClient)r).SetEx(keys[index0], 100, GetBytes(values[index0])));
            }

            pipeline.Flush();
            pipeline.Replay();


            for (int i = 0; i < 3; ++i)
                Assert.AreEqual(Redis.GetValue(keys[i]), values[i]);

            pipeline.Dispose();
        }

        [Test]
        public void Can_call_single_operation_with_callback_3_Times_in_pipeline()
        {
            var results = new List<long>();
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key), results.Add);
                pipeline.QueueCommand(r => r.IncrementValue(Key), results.Add);
                pipeline.QueueCommand(r => r.IncrementValue(Key), results.Add);

                pipeline.Flush();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("3"));
            Assert.That(results, Is.EquivalentTo(new List<long> { 1, 2, 3 }));
        }

        [Test]
        public void Supports_different_operation_types_in_same_pipeline()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));
                pipeline.QueueCommand(r => r.AddItemToList(ListKey, "listitem1"));
                pipeline.QueueCommand(r => r.AddItemToList(ListKey, "listitem2"));
                pipeline.QueueCommand(r => r.AddItemToSet(SetKey, "setitem"));
                pipeline.QueueCommand(r => r.SetContainsItem(SetKey, "setitem"), b => containsItem = b);
                pipeline.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem1"));
                pipeline.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem2"));
                pipeline.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem3"));
                pipeline.QueueCommand(r => r.GetListCount(ListKey), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSetCount(SetKey), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSortedSetCount(SortedSetKey), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));

                pipeline.Flush();
            }

            Assert.That(containsItem, Is.True);
            Assert.That(Redis.GetValue(Key), Is.EqualTo("2"));
            Assert.That(incrementResults, Is.EquivalentTo(new List<long> { 1, 2 }));
            Assert.That(collectionCounts, Is.EquivalentTo(new List<int> { 2, 1, 3 }));
            Assert.That(Redis.GetAllItemsFromList(ListKey), Is.EquivalentTo(new List<string> { "listitem1", "listitem2" }));
            Assert.That(Redis.GetAllItemsFromSet(SetKey), Is.EquivalentTo(new List<string> { "setitem" }));
            Assert.That(Redis.GetAllItemsFromSortedSet(SortedSetKey), Is.EquivalentTo(new List<string> { "sortedsetitem1", "sortedsetitem2", "sortedsetitem3" }));
        }

        [Test]
        public void Can_call_multi_string_operations_in_pipeline()
        {
            string item1 = null;
            string item4 = null;

            var results = new List<string>();
            Assert.That(Redis.GetListCount(ListKey), Is.EqualTo(0));
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToList(ListKey, "listitem1"));
                pipeline.QueueCommand(r => r.AddItemToList(ListKey, "listitem2"));
                pipeline.QueueCommand(r => r.AddItemToList(ListKey, "listitem3"));
                pipeline.QueueCommand(r => r.GetAllItemsFromList(ListKey), x => results = x);
                pipeline.QueueCommand(r => r.GetItemFromList(ListKey, 0), x => item1 = x);
                pipeline.QueueCommand(r => r.GetItemFromList(ListKey, 4), x => item4 = x);

                pipeline.Flush();
            }

            Assert.That(Redis.GetListCount(ListKey), Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<string> { "listitem1", "listitem2", "listitem3" }));
            Assert.That(item1, Is.EqualTo("listitem1"));
            Assert.That(item4, Is.Null);
        }
        [Test]
        // Operations that are not supported in older versions will look at server info to determine what to do.
        // If server info is fetched each time, then it will interfer with pipeline
        public void Can_call_operation_not_supported_on_older_servers_in_pipeline()
        {
            var temp = new byte[1];
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => ((RedisNativeClient)r).SetEx(Key + "key", 5, temp));
                pipeline.Flush();
            }
        }
        [Test]
        public void Pipeline_can_be_replayed()
        {
            string KeySquared = Key + Key;
            Assert.That(Redis.GetValue(Key), Is.Null);
            Assert.That(Redis.GetValue(KeySquared), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key));
                pipeline.QueueCommand(r => r.IncrementValue(KeySquared));
                pipeline.Flush();

                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(KeySquared), Is.EqualTo("1"));
                Redis.Del(Key);
                Redis.Del(KeySquared);
                Assert.That(Redis.GetValue(Key), Is.Null);
                Assert.That(Redis.GetValue(KeySquared), Is.Null);

                pipeline.Replay();
                pipeline.Dispose();
                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(KeySquared), Is.EqualTo("1"));
            }
        }

        [Test]
        public void Pipeline_can_be_contain_watch()
        {
            string KeySquared = Key + Key;
            Assert.That(Redis.GetValue(Key), Is.Null);
            Assert.That(Redis.GetValue(KeySquared), Is.Null);
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValue(Key));
                pipeline.QueueCommand(r => r.IncrementValue(KeySquared));
                pipeline.QueueCommand(r => ((RedisNativeClient)r).Watch(Key + "FOO"));
                pipeline.Flush();

                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(KeySquared), Is.EqualTo("1"));
            }
        }

        [Test]
        public void Can_call_AddRangeToSet_in_pipeline()
        {
            using (var pipeline = Redis.CreatePipeline())
            {
                var key = "pipeline-test";

                pipeline.QueueCommand(r => r.Remove(key));
                pipeline.QueueCommand(r => r.AddRangeToSet(key, new[] { "A", "B", "C" }.ToList()));

                pipeline.Flush();
            }
        }
    }
}