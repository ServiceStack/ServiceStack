using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisTransactionTestsAsync
        : RedisClientTestsBaseAsync
    {
        private const string Key = "rdtmultitest";
        private const string ListKey = "rdtmultitest-list";
        private const string SetKey = "rdtmultitest-set";
        private const string SortedSetKey = "rdtmultitest-sortedset";
        private const string HashKey = "rdthashtest";

        public override void OnAfterEachTest()
        {
            CleanMask = Key + "*";
            base.OnAfterEachTest();
        }

        [Test]
        public async Task Can_call_single_operation_in_transaction()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key));
                var map = new Dictionary<string, int>();
                trans.QueueCommand(r => r.GetAsync<int>(Key).AsValueTask(), y => map[Key] = y);

                await trans.CommitAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("1"));
        }

        [Test]
        public async Task No_commit_of_atomic_transactions_discards_all_commands()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key));
            }
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
        }

        [Test]
        public async Task Watch_aborts_transaction()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            const string value1 = "value1";
            try
            {
                await RedisAsync.WatchAsync(new[] { Key });
                await RedisAsync.SetAsync(Key, value1);
                await using var trans = await RedisAsync.CreateTransactionAsync();
                trans.QueueCommand(r => r.SetAsync(Key, value1).AsValueTask());
                var success = await trans.CommitAsync();
                Assert.False(success);
                Assert.AreEqual(value1, await RedisAsync.GetAsync<string>(Key));
            }
            catch (NotSupportedException)
            {
                Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            }
        }

        [Test]
        public async Task Exception_in_atomic_transactions_discards_all_commands()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            try
            {
                await using var trans = await RedisAsync.CreateTransactionAsync();
                trans.QueueCommand(r => r.IncrementValueAsync(Key));
                throw new NotSupportedException();
            }
            catch (NotSupportedException)
            {
                Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            }
        }

        [Test]
        public async Task Can_call_single_operation_3_Times_in_transaction()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key));
                trans.QueueCommand(r => r.IncrementValueAsync(Key));
                trans.QueueCommand(r => r.IncrementValueAsync(Key));

                await trans.CommitAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("3"));
        }

        [Test]
        public async Task Can_call_single_operation_with_callback_3_Times_in_transaction()
        {
            var results = new List<long>();
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key), results.Add);
                trans.QueueCommand(r => r.IncrementValueAsync(Key), results.Add);
                trans.QueueCommand(r => r.IncrementValueAsync(Key), results.Add);

                await trans.CommitAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("3"));
            Assert.That(results, Is.EquivalentTo(new List<long> { 1, 2, 3 }));
        }

        [Test]
        public async Task Supports_different_operation_types_in_same_transaction()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));
                trans.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem1"));
                trans.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem2"));
                trans.QueueCommand(r => r.AddItemToSetAsync(SetKey, "setitem"));
                trans.QueueCommand(r => r.SetContainsItemAsync(SetKey, "setitem"), b => containsItem = b);
                trans.QueueCommand(r => r.AddItemToSortedSetAsync(SortedSetKey, "sortedsetitem1"));
                trans.QueueCommand(r => r.AddItemToSortedSetAsync(SortedSetKey, "sortedsetitem2"));
                trans.QueueCommand(r => r.AddItemToSortedSetAsync(SortedSetKey, "sortedsetitem3"));
                trans.QueueCommand(r => r.GetListCountAsync(ListKey), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.GetSetCountAsync(SetKey), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.GetSortedSetCountAsync(SortedSetKey), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.IncrementValueAsync(Key), intResult => incrementResults.Add(intResult));

                await trans.CommitAsync();
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
        public async Task Can_call_multi_string_operations_in_transaction()
        {
            string item1 = null;
            string item4 = null;

            var results = new List<string>();
            Assert.That(await RedisAsync.GetListCountAsync(ListKey), Is.EqualTo(0));
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem1"));
                trans.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem2"));
                trans.QueueCommand(r => r.AddItemToListAsync(ListKey, "listitem3"));
                trans.QueueCommand(r => r.GetAllItemsFromListAsync(ListKey), x => results = x);
                trans.QueueCommand(r => r.GetItemFromListAsync(ListKey, 0), x => item1 = x);
                trans.QueueCommand(r => r.GetItemFromListAsync(ListKey, 4), x => item4 = x);

                await trans.CommitAsync();
            }

            Assert.That(await RedisAsync.GetListCountAsync(ListKey), Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<string> { "listitem1", "listitem2", "listitem3" }));
            Assert.That(item1, Is.EqualTo("listitem1"));
            Assert.That(item4, Is.Null);
        }
        [Test]
        public async Task Can_call_multiple_setexs_in_transaction()
        {
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);
            var keys = new[] { "key1", "key2", "key3" };
            var values = new[] { "1", "2", "3" };
            await using var trans = await RedisAsync.CreateTransactionAsync();

            for (int i = 0; i < 3; ++i)
            {
                int index0 = i;
                trans.QueueCommand(r => ((IRedisNativeClientAsync)r).SetExAsync(keys[index0], 100, GetBytes(values[index0])));
            }

            await trans.CommitAsync();
            await trans.ReplayAsync();


            for (int i = 0; i < 3; ++i)
                Assert.AreEqual(await RedisAsync.GetValueAsync(keys[i]), values[i]);
        }
        [Test]
        // Operations that are not supported in older versions will look at server info to determine what to do.
        // If server info is fetched each time, then it will interfer with transaction
        public async Task Can_call_operation_not_supported_on_older_servers_in_transaction()
        {
            var temp = new byte[1];
            await using var trans = await RedisAsync.CreateTransactionAsync();
            trans.QueueCommand(r => ((IRedisNativeClientAsync)r).SetExAsync(Key, 5, temp));
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

        [Test]
        public async Task Transaction_can_issue_watch()
        {
            await NativeAsync.DelAsync(Key);
            Assert.That(await RedisAsync.GetValueAsync(Key), Is.Null);

            string KeySquared = Key + Key;
            await NativeAsync.DelAsync(KeySquared);

            await RedisAsync.WatchAsync(new[] { Key, KeySquared });
            await RedisAsync.SetAsync(Key, 7);

            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.SetAsync(Key, 1).AsValueTask());
                trans.QueueCommand(r => r.SetAsync(KeySquared, 2).AsValueTask());
                await trans.CommitAsync();
            }

            Assert.That(await RedisAsync.GetValueAsync(Key), Is.EqualTo("7"));
            Assert.That(await RedisAsync.GetValueAsync(KeySquared), Is.Null);
        }

        [Test]
        public async Task Can_set_Expiry_on_key_in_transaction()
        {
            var expiresIn = TimeSpan.FromMinutes(15);

            const string key = "No TTL-Transaction";
            var keyWithTtl = "{0}s TTL-Transaction".Fmt(expiresIn.TotalSeconds);

            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.AddAsync(key, "Foo").AsValueTask());
                trans.QueueCommand(r => r.AddAsync(keyWithTtl, "Bar", expiresIn).AsValueTask());

                if (!await trans.CommitAsync())
                    throw new Exception("Transaction Failed");
            }

            Assert.That(await RedisAsync.GetAsync<string>(key), Is.EqualTo("Foo"));
            Assert.That(await RedisAsync.GetAsync<string>(keyWithTtl), Is.EqualTo("Bar"));

            Assert.That(await RedisAsync.GetTimeToLiveAsync(key), Is.EqualTo(TimeSpan.MaxValue));
            Assert.That((await RedisAsync.GetTimeToLiveAsync(keyWithTtl)).Value.TotalSeconds, Is.GreaterThan(1));
        }

        [Test]
        public async Task Does_not_set_Expiry_on_existing_key_in_transaction()
        {
            var expiresIn = TimeSpan.FromMinutes(15);

            var key = "Exting TTL-Transaction";
            await RedisAsync.AddAsync(key, "Foo");

            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.AddAsync(key, "Bar", expiresIn).AsValueTask());

                if (!await trans.CommitAsync())
                    throw new Exception("Transaction Failed");
            }

            Assert.That(await RedisAsync.GetAsync<string>(key), Is.EqualTo("Foo"));
            Assert.That(await RedisAsync.GetTimeToLiveAsync(key), Is.EqualTo(TimeSpan.MaxValue));
        }

        [Test]
        public async Task Can_call_GetAllEntriesFromHash_in_transaction()
        {
            var stringMap = new Dictionary<string, string> {
                 {"one","a"}, {"two","b"}, {"three","c"}, {"four","d"}
             };
            foreach (var x in stringMap)
            {
                await RedisAsync.SetEntryInHashAsync(HashKey, x.Key, x.Value);
            }

            Dictionary<string, string> results = null;
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.GetAllEntriesFromHashAsync(HashKey), x => results = x);

                await trans.CommitAsync();
            }

            Assert.That(results, Is.EquivalentTo(stringMap));
        }

        [Test]
        public async Task Can_call_Type_in_transaction()
        {
            await RedisAsync.SetValueAsync("string", "STRING");
            await RedisAsync.AddItemToListAsync("list", "LIST");
            await RedisAsync.AddItemToSetAsync("set", "SET");
            await RedisAsync.AddItemToSortedSetAsync("zset", "ZSET", 1);

            var keys = new[] { "string", "list", "set", "zset" };

            var results = new Dictionary<string, string>();
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                foreach (var key in keys)
                {
                    trans.QueueCommand(r => r.TypeAsync(key), x => results[key] = x);
                }

                await trans.CommitAsync();
            }

            results.PrintDump();

            Assert.That(results, Is.EquivalentTo(new Dictionary<string, string>
            {
                {"string", "string" },
                {"list", "list" },
                {"set", "set" },
                {"zset", "zset" },
            }));
        }

        [Test]
        public async Task Can_call_HashSet_commands_in_transaction()
        {
            await RedisAsync.AddItemToSetAsync("set", "ITEM 1");
            await RedisAsync.AddItemToSetAsync("set", "ITEM 2");
            HashSet<string> result = null;

            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.GetAllItemsFromSetAsync("set"), values => result = values);

                await trans.CommitAsync();
            }

            Assert.That(result, Is.EquivalentTo(new[] { "ITEM 1", "ITEM 2" }));
        }

        [Test]
        public async Task Can_call_LUA_Script_in_transaction()
        {
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.ExecLuaAsync("return {'myval', 'myotherval'}", new string[0]));

                await trans.CommitAsync();
            }

            RedisText result = null;
            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.ExecLuaAsync("return {'myval', 'myotherval'}", new string[0]), s => result = s);

                await trans.CommitAsync();
            }

            Assert.That(result.Children[0].Text, Is.EqualTo("myval"));
            Assert.That(result.Children[1].Text, Is.EqualTo("myotherval"));
        }

        [Test]
        public async Task Can_call_SetValueIfNotExists_in_transaction()
        {
            bool f = false;
            bool s = false;

            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(c => c.SetValueIfNotExistsAsync("foo", "blah"), r => f = r);
                trans.QueueCommand(c => c.SetValueIfNotExistsAsync("bar", "blah"), r => s = r);
                await trans.CommitAsync();
            }

            Assert.That(f);
            Assert.That(s);
        }
    }
}