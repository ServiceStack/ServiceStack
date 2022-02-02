using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisTransactionTests
        : RedisClientTestsBase
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
        public void Can_call_single_operation_in_transaction()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue(Key));
                var map = new Dictionary<string, int>();
                trans.QueueCommand(r => r.Get<int>(Key), y => map[Key] = y);

                trans.Commit();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
        }

        [Test]
        public void No_commit_of_atomic_transactions_discards_all_commands()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue(Key));
            }
            Assert.That(Redis.GetValue(Key), Is.Null);
        }

        [Test]
        public void Watch_aborts_transaction()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            const string value1 = "value1";
            try
            {
                Redis.Watch(Key);
                Redis.Set(Key, value1);
                using (var trans = Redis.CreateTransaction())
                {
                    trans.QueueCommand(r => r.Set(Key, value1));
                    var success = trans.Commit();
                    Assert.False(success);
                    Assert.AreEqual(value1, Redis.Get<string>(Key));
                }
            }
            catch (NotSupportedException)
            {
                Assert.That(Redis.GetValue(Key), Is.Null);
            }
        }

        [Test]
        public void Exception_in_atomic_transactions_discards_all_commands()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            try
            {
                using (var trans = Redis.CreateTransaction())
                {
                    trans.QueueCommand(r => r.IncrementValue(Key));
                    throw new NotSupportedException();
                }
            }
            catch (NotSupportedException)
            {
                Assert.That(Redis.GetValue(Key), Is.Null);
            }
        }

        [Test]
        public void Can_call_single_operation_3_Times_in_transaction()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue(Key));
                trans.QueueCommand(r => r.IncrementValue(Key));
                trans.QueueCommand(r => r.IncrementValue(Key));

                trans.Commit();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("3"));
        }

        [Test]
        public void Can_call_single_operation_with_callback_3_Times_in_transaction()
        {
            var results = new List<long>();
            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue(Key), results.Add);
                trans.QueueCommand(r => r.IncrementValue(Key), results.Add);
                trans.QueueCommand(r => r.IncrementValue(Key), results.Add);

                trans.Commit();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("3"));
            Assert.That(results, Is.EquivalentTo(new List<long> { 1, 2, 3 }));
        }

        [Test]
        public void Supports_different_operation_types_in_same_transaction()
        {
            var incrementResults = new List<long>();
            var collectionCounts = new List<long>();
            var containsItem = false;

            Assert.That(Redis.GetValue(Key), Is.Null);
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));
                trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem1"));
                trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem2"));
                trans.QueueCommand(r => r.AddItemToSet(SetKey, "setitem"));
                trans.QueueCommand(r => r.SetContainsItem(SetKey, "setitem"), b => containsItem = b);
                trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem1"));
                trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem2"));
                trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem3"));
                trans.QueueCommand(r => r.GetListCount(ListKey), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.GetSetCount(SetKey), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.GetSortedSetCount(SortedSetKey), intResult => collectionCounts.Add(intResult));
                trans.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));

                trans.Commit();
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
        public void Can_call_multi_string_operations_in_transaction()
        {
            string item1 = null;
            string item4 = null;

            var results = new List<string>();
            Assert.That(Redis.GetListCount(ListKey), Is.EqualTo(0));
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem1"));
                trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem2"));
                trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem3"));
                trans.QueueCommand(r => r.GetAllItemsFromList(ListKey), x => results = x);
                trans.QueueCommand(r => r.GetItemFromList(ListKey, 0), x => item1 = x);
                trans.QueueCommand(r => r.GetItemFromList(ListKey, 4), x => item4 = x);

                trans.Commit();
            }

            Assert.That(Redis.GetListCount(ListKey), Is.EqualTo(3));
            Assert.That(results, Is.EquivalentTo(new List<string> { "listitem1", "listitem2", "listitem3" }));
            Assert.That(item1, Is.EqualTo("listitem1"));
            Assert.That(item4, Is.Null);
        }
        [Test]
        public void Can_call_multiple_setexs_in_transaction()
        {
            Assert.That(Redis.GetValue(Key), Is.Null);
            var keys = new[] { "key1", "key2", "key3" };
            var values = new[] { "1", "2", "3" };
            var trans = Redis.CreateTransaction();

            for (int i = 0; i < 3; ++i)
            {
                int index0 = i;
                trans.QueueCommand(r => ((RedisNativeClient)r).SetEx(keys[index0], 100, GetBytes(values[index0])));
            }

            trans.Commit();
            trans.Replay();


            for (int i = 0; i < 3; ++i)
                Assert.AreEqual(Redis.GetValue(keys[i]), values[i]);

            trans.Dispose();
        }
        [Test]
        // Operations that are not supported in older versions will look at server info to determine what to do.
        // If server info is fetched each time, then it will interfer with transaction
        public void Can_call_operation_not_supported_on_older_servers_in_transaction()
        {
            var temp = new byte[1];
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => ((RedisNativeClient)r).SetEx(Key, 5, temp));
                trans.Commit();
            }
        }


        [Test]
        public void Transaction_can_be_replayed()
        {
            string KeySquared = Key + Key;
            Assert.That(Redis.GetValue(Key), Is.Null);
            Assert.That(Redis.GetValue(KeySquared), Is.Null);
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.IncrementValue(Key));
                trans.QueueCommand(r => r.IncrementValue(KeySquared));
                trans.Commit();

                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(KeySquared), Is.EqualTo("1"));
                Redis.Del(Key);
                Redis.Del(KeySquared);
                Assert.That(Redis.GetValue(Key), Is.Null);
                Assert.That(Redis.GetValue(KeySquared), Is.Null);

                trans.Replay();
                trans.Dispose();
                Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
                Assert.That(Redis.GetValue(KeySquared), Is.EqualTo("1"));
            }
        }

        [Test]
        public void Transaction_can_issue_watch()
        {
            Redis.Del(Key);
            Assert.That(Redis.GetValue(Key), Is.Null);

            string KeySquared = Key + Key;
            Redis.Del(KeySquared);

            Redis.Watch(Key, KeySquared);
            Redis.Set(Key, 7);

            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.Set(Key, 1));
                trans.QueueCommand(r => r.Set(KeySquared, 2));
                trans.Commit();
            }

            Assert.That(Redis.GetValue(Key), Is.EqualTo("7"));
            Assert.That(Redis.GetValue(KeySquared), Is.Null);
        }

        [Test]
        public void Can_set_Expiry_on_key_in_transaction()
        {
            var expiresIn = TimeSpan.FromMinutes(15);

            const string key = "No TTL-Transaction";
            var keyWithTtl = "{0}s TTL-Transaction".Fmt(expiresIn.TotalSeconds);

            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.Add(key, "Foo"));
                trans.QueueCommand(r => r.Add(keyWithTtl, "Bar", expiresIn));

                if (!trans.Commit())
                    throw new Exception("Transaction Failed");
            }

            Assert.That(Redis.Get<string>(key), Is.EqualTo("Foo"));
            Assert.That(Redis.Get<string>(keyWithTtl), Is.EqualTo("Bar"));

            Assert.That(Redis.GetTimeToLive(key), Is.EqualTo(TimeSpan.MaxValue));
            Assert.That(Redis.GetTimeToLive(keyWithTtl).Value.TotalSeconds, Is.GreaterThan(1));
        }

        [Test]
        public void Does_not_set_Expiry_on_existing_key_in_transaction()
        {
            var expiresIn = TimeSpan.FromMinutes(15);

            var key = "Exting TTL-Transaction";
            Redis.Add(key, "Foo");

            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.Add(key, "Bar", expiresIn));

                if (!trans.Commit())
                    throw new Exception("Transaction Failed");
            }

            Assert.That(Redis.Get<string>(key), Is.EqualTo("Foo"));
            Assert.That(Redis.GetTimeToLive(key), Is.EqualTo(TimeSpan.MaxValue));
        }

        [Test]
        public void Can_call_GetAllEntriesFromHash_in_transaction()
        {
            var stringMap = new Dictionary<string, string> {
                 {"one","a"}, {"two","b"}, {"three","c"}, {"four","d"}
             };
            stringMap.Each(x => Redis.SetEntryInHash(HashKey, x.Key, x.Value));

            Dictionary<string, string> results = null;
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.GetAllEntriesFromHash(HashKey), x => results = x);

                trans.Commit();
            }

            Assert.That(results, Is.EquivalentTo(stringMap));
        }

        [Test]
        public void Can_call_Type_in_transaction()
        {
            Redis.SetValue("string", "STRING");
            Redis.AddItemToList("list", "LIST");
            Redis.AddItemToSet("set", "SET");
            Redis.AddItemToSortedSet("zset", "ZSET", 1);

            var keys = new[] { "string", "list", "set", "zset" };

            var results = new Dictionary<string, string>();
            using (var trans = Redis.CreateTransaction())
            {
                foreach (var key in keys)
                {
                    trans.QueueCommand(r => r.Type(key), x => results[key] = x);
                }

                trans.Commit();
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
        public void Can_call_HashSet_commands_in_transaction()
        {
            Redis.AddItemToSet("set", "ITEM 1");
            Redis.AddItemToSet("set", "ITEM 2");
            HashSet<string> result = null;

            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.GetAllItemsFromSet("set"), values => result = values);

                trans.Commit();
            }

            Assert.That(result, Is.EquivalentTo(new[] { "ITEM 1", "ITEM 2" }));
        }

        [Test]
        public void Can_call_LUA_Script_in_transaction()
        {
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.ExecLua("return {'myval', 'myotherval'}"));

                trans.Commit();
            }

            RedisText result = null;
            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.ExecLua("return {'myval', 'myotherval'}"), s => result = s);

                trans.Commit();
            }

            Assert.That(result.Children[0].Text, Is.EqualTo("myval"));
            Assert.That(result.Children[1].Text, Is.EqualTo("myotherval"));
        }

        [Test]
        public void Can_call_SetValueIfNotExists_in_transaction()
        {
            bool f = false;
            bool s = false;

            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(c => c.SetValueIfNotExists("foo", "blah"), r => f = r);
                trans.QueueCommand(c => c.SetValueIfNotExists("bar", "blah"), r => s = r);
                trans.Commit();
            }

            Assert.That(f);
            Assert.That(s);
        }
    }
}