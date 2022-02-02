using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisPipelineTestsAsync
        : RedisClientTestsBaseAsync
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
        public async Task Can_call_single_operation_in_pipeline()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
                var map = new Dictionary<string, int>();
                pipeline.QueueCommand(r => r.GetAsync<int>(Key).AsValueTask(), y => map[Key] = y);

                await pipeline.FlushAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
        }

        [Test]
        public async Task No_commit_of_atomic_pipelines_discards_all_commands()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
            }
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
        }

        [Test]
        public async Task Exception_in_atomic_pipelines_discards_all_commands()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            try
            {
                await using var pipeline = RedisAsync.CreatePipeline();
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
                throw new NotSupportedException();
            }
            catch (NotSupportedException)
            {
                Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            }
        }

        [Test]
        public async Task Can_call_single_operation_3_Times_in_pipeline()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key));

                await pipeline.FlushAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("3"));
        }
        [Test]
        public async Task Can_call_hash_operations_in_pipeline()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
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
            await using var pipeline = RedisAsync.CreatePipeline();


            pipeline.QueueCommand(r => ((IRedisNativeClientAsync)r).HMSetAsync(Key, fieldBytes, valueBytes));
            pipeline.QueueCommand(r => ((IRedisNativeClientAsync)r).HGetAllAsync(Key), x => members = x);


            await pipeline.FlushAsync();


            for (var i = 0; i < members.Length; i += 2)
            {
                Assert.AreEqual(members[i], fieldBytes[i / 2]);
                Assert.AreEqual(members[i + 1], valueBytes[i / 2]);

            }
        }

        [Test]
        public async Task Can_call_multiple_setexs_in_pipeline()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            var keys = new[] { Key + "key1", Key + "key2", Key + "key3" };
            var values = new[] { "1", "2", "3" };
            await using var pipeline = RedisAsync.CreatePipeline();

            for (int i = 0; i < 3; ++i)
            {
                int index0 = i;
                pipeline.QueueCommand(r => ((IRedisNativeClientAsync)r).SetExAsync(keys[index0], 100, GetBytes(values[index0])));
            }

            await pipeline.FlushAsync();
            await pipeline.ReplayAsync();


            for (int i = 0; i < 3; ++i)
                Assert.AreEqual(await RedisAsync.GetValueAsync(keys[i]), values[i]);
        }

        [Test]
        public async Task Can_call_single_operation_with_callback_3_Times_in_pipeline()
        {
            var results = new List<long>();
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), results.Add);
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), results.Add);
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), results.Add);

                await pipeline.FlushAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("3"));
            Assert.That(results, Is.EquivalentTo(new List<long> { 1, 2, 3 }));
        }

        [Test]
        public async Task Supports_different_operation_types_in_same_pipeline()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));
                pipeline.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem1"));
                pipeline.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem2"));
                pipeline.QueueCommand(r => r.AddItemToSetAsync(SetKey, "setitem"));
                pipeline.QueueCommand(r => r.SetContainsItemAsync(SetKey, "setitem"), b => containsItem = b);
                pipeline.QueueCommand(r => r.AddItemToSortedSetAsync(SortedSetKey, "sortedsetitem1"));
                pipeline.QueueCommand(r => r.AddItemToSortedSetAsync(SortedSetKey, "sortedsetitem2"));
                pipeline.QueueCommand(r => r.AddItemToSortedSetAsync(SortedSetKey, "sortedsetitem3"));
                pipeline.QueueCommand(r => r.GetListCountAsync(ListKey), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSetCountAsync(SetKey), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.GetSortedSetCountAsync(SortedSetKey), intResult => collectionCounts.Add(intResult));
                pipeline.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));

                await pipeline.FlushAsync();
            }

            Assert.That(containsItem, Is.True);
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("2"));
            Assert.That(incrementResults, Is.EquivalentTo(new List<long> { 1, 2 }));
            Assert.That(collectionCounts, Is.EquivalentTo(new List<int> { 2, 1, 3 }));
            Assert.That(await RedisAsync.GetAllItemsFromListAsync(ListKey), Is.EquivalentTo(new List<string> { "listitem1", "listitem2" }));
            Assert.That(await RedisAsync.GetAllItemsFromSetAsync(SetKey), Is.EquivalentTo(new List<string> { "setitem" }));
            Assert.That(await RedisAsync.GetAllItemsFromSortedSetAsync(SortedSetKey), Is.EquivalentTo(new List<string> { "sortedsetitem1", "sortedsetitem2", "sortedsetitem3" }));
        }

        [Test]
        public async Task Can_call_multi_string_operations_in_pipeline()
        {
            string item1 = null;
            string item4 = null;

            var results = new List<string>();
            Assert.That(await RedisAsync.GetListCountAsync(ListKey), Is.EqualTo(0));
            await using (var pipeline = RedisAsync.CreatePipeline())
            {
                pipeline.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem1"));
                pipeline.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem2"));
                pipeline.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem3"));
                pipeline.QueueCommand(r => r.GetAllItemsFromListAsync(ListKey), x => results = x);
                pipeline.QueueCommand(r => r.GetItemFromListAsync(ListKey, 0), x => item1 = x);
                pipeline.QueueCommand(r => r.GetItemFromListAsync(ListKey, 4), x => item4 = x);

                await pipeline.FlushAsync();
            }

            Assert.That(await RedisAsync.GetListCountAsync(ListKey), Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<string> { "listitem1", "listitem2", "listitem3" }));
            Assert.That(item1, Is.EqualTo("listitem1"));
            Assert.That(item4, Is.Null);
        }
        [Test]
        // Operations that are not supported in older versions will look at server info to determine what to do.
        // If server info is fetched each time, then it will interfer with pipeline
        public async Task Can_call_operation_not_supported_on_older_servers_in_pipeline()
        {
            var temp = new byte[1];
            await using var pipeline = RedisAsync.CreatePipeline();
            pipeline.QueueCommand(r => ((IRedisNativeClientAsync)r).SetExAsync(Key + "key", 5, temp));
            await pipeline.FlushAsync();
        }
        [Test]
        public async Task Pipeline_can_be_replayed()
        {
            string KeySquared = Key + Key;
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.Null);
            await using var pipeline = RedisAsync.CreatePipeline();
            pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
            pipeline.QueueCommand(r => r.IncrementValueAsync(KeySquared));
            await pipeline.FlushAsync();

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.EqualTo("1"));
            await NativeAsync.DelAsync(Key);
            await NativeAsync.DelAsync(KeySquared);
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.Null);

            await pipeline.ReplayAsync();
            await pipeline.DisposeAsync();
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.EqualTo("1"));
        }

        [Test]
        public async Task Pipeline_can_be_contain_watch()
        {
            string KeySquared = Key + Key;
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.Null);
            await using var pipeline = RedisAsync.CreatePipeline();
            pipeline.QueueCommand(r => r.IncrementValueAsync(Key));
            pipeline.QueueCommand(r => r.IncrementValueAsync(KeySquared));
            pipeline.QueueCommand(r => ((IRedisNativeClientAsync)r).WatchAsync(new[] { Key + "FOO" }));
            await pipeline.FlushAsync();

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.EqualTo("1"));
        }

        [Test]
        public async Task Can_call_AddRangeToSet_in_pipeline()
        {
            await using var pipeline = RedisAsync.CreatePipeline();
            var key = "pipeline-test";

            pipeline.QueueCommand(r => r.RemoveAsync(key).AsValueTask());
            pipeline.QueueCommand(r => r.AddRangeToSetAsync(key, new[] { "A", "B", "C" }.ToList()));

            await pipeline.FlushAsync();
        }
    }
}