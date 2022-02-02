using NUnit.Framework;
using ServiceStack.Redis.Support.Locking;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisClientTestsAsync
        : RedisClientTestsBaseAsync
    {
        const string Value = "Value";

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            RedisRaw.NamespacePrefix = nameof(RedisClientTestsAsync);
        }

        [Test]
        public async Task Can_Set_and_Get_string()
        {
            await RedisAsync.SetValueAsync("key", Value);
            var valueBytes = await NativeAsync.GetAsync("key");
            var valueString = GetString(valueBytes);
            await RedisAsync.RemoveAsync("key");

            Assert.That(valueString, Is.EqualTo(Value));
        }

        [Test]
        public async Task Can_Set_and_Get_key_with_space()
        {
            await RedisAsync.SetValueAsync("key with space", Value);
            var valueBytes = await NativeAsync.GetAsync("key with space");
            var valueString = GetString(valueBytes);
            await RedisAsync.RemoveAsync("key with space");

            Assert.That(valueString, Is.EqualTo(Value));
        }

        [Test]
        public async Task Can_Set_and_Get_key_with_spaces()
        {
            const string key = "key with spaces";

            await RedisAsync.SetValueAsync(key, Value);
            var valueBytes = await NativeAsync.GetAsync(key);
            var valueString = GetString(valueBytes);

            Assert.That(valueString, Is.EqualTo(Value));
        }

        [Test]
        public async Task Can_Set_and_Get_key_with_all_byte_values()
        {
            const string key = "bytesKey";

            var value = new byte[256];
            for (var i = 0; i < value.Length; i++)
            {
                value[i] = (byte)i;
            }

            await RedisAsync.SetAsync(key, value);
            var resultValue = await NativeAsync.GetAsync(key);

            Assert.That(resultValue, Is.EquivalentTo(value));
        }

        [Test]
        public async Task GetKeys_returns_matching_collection()
        {
            await RedisAsync.SetAsync("ss-tests:a1", "One");
            await RedisAsync.SetAsync("ss-tests:a2", "One");
            await RedisAsync.SetAsync("ss-tests:b3", "One");

            var matchingKeys = await RedisAsync.SearchKeysAsync("ss-tests:a*");

            Assert.That(matchingKeys.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetKeys_on_non_existent_keys_returns_empty_collection()
        {
            var matchingKeys = await RedisAsync.SearchKeysAsync("ss-tests:NOTEXISTS");

            Assert.That(matchingKeys.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Can_get_Types()
        {
            await RedisAsync.SetValueAsync("string", "string");
            await RedisAsync.AddItemToListAsync("list", "list");
            await RedisAsync.AddItemToSetAsync("set", "set");
            await RedisAsync.AddItemToSortedSetAsync("sortedset", "sortedset");
            await RedisAsync.SetEntryInHashAsync("hash", "key", "val");

            Assert.That(await RedisAsync.GetEntryTypeAsync("nokey"), Is.EqualTo(RedisKeyType.None));
            Assert.That(await RedisAsync.GetEntryTypeAsync("string"), Is.EqualTo(RedisKeyType.String));
            Assert.That(await RedisAsync.GetEntryTypeAsync("list"), Is.EqualTo(RedisKeyType.List));
            Assert.That(await RedisAsync.GetEntryTypeAsync("set"), Is.EqualTo(RedisKeyType.Set));
            Assert.That(await RedisAsync.GetEntryTypeAsync("sortedset"), Is.EqualTo(RedisKeyType.SortedSet));
            Assert.That(await RedisAsync.GetEntryTypeAsync("hash"), Is.EqualTo(RedisKeyType.Hash));
        }

        [Test]
        public async Task Can_delete_keys()
        {
            await RedisAsync.SetValueAsync("key", "val");

            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);

            await RedisAsync.RemoveAsync("key");

            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);

            var keysMap = new Dictionary<string, string>();

            10.Times(i => keysMap.Add("key" + i, "val" + i));

            await RedisAsync.SetAllAsync(keysMap);

            for (int i = 0; i < 10; i++)
                Assert.That(await RedisAsync.ContainsKeyAsync("key" + i), Is.True);

            await RedisAsync.RemoveEntryAsync(keysMap.Keys.ToArray());

            for (int i = 0; i < 10; i++)
                Assert.That(await RedisAsync.ContainsKeyAsync("key" + i), Is.False);
        }

        [Test]
        public async Task Can_get_RandomKey()
        {
            await RedisAsync.SelectAsync(15);
            var keysMap = new Dictionary<string, string>();

            10.Times(i => keysMap.Add(RedisRaw.NamespacePrefix + "key" + i, "val" + i));

            await RedisAsync.SetAllAsync(keysMap);

            var randKey = await RedisAsync.GetRandomKeyAsync();

            Assert.That(keysMap.ContainsKey(randKey), Is.True);
        }

        [Test]
        public async Task Can_RenameKey()
        {
            await RedisAsync.SetValueAsync("oldkey", "val");
            await NativeAsync.RenameAsync("oldkey", "newkey");

            Assert.That(await RedisAsync.ContainsKeyAsync("oldkey"), Is.False);
            Assert.That(await RedisAsync.ContainsKeyAsync("newkey"), Is.True);
        }

        [Test]
        public async Task Can_Expire()
        {
            await RedisAsync.SetValueAsync("key", "val");
            await RedisAsync.ExpireEntryInAsync("key", TimeSpan.FromSeconds(1));
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);
            await Task.Delay(2000);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);
        }

        [Test]
        public async Task Can_Expire_Ms()
        {
            await RedisAsync.SetValueAsync("key", "val");
            await RedisAsync.ExpireEntryInAsync("key", TimeSpan.FromMilliseconds(100));
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);
            await Task.Delay(500);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);
        }

        [Ignore("Changes in system clock can break test")]
        [Test]
        public async Task Can_ExpireAt()
        {
            await RedisAsync.SetValueAsync("key", "val");

            var unixNow = DateTime.Now.ToUnixTime();
            var in2Secs = unixNow + 2;

            await NativeAsync.ExpireAtAsync("key", in2Secs);

            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);
            await Task.Delay(3000);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);
        }

        [Test]
        public async Task Can_GetTimeToLive()
        {
            await RedisAsync.SetValueAsync("key", "val");
            await RedisAsync.ExpireEntryInAsync("key", TimeSpan.FromSeconds(10));

            var ttl = await RedisAsync.GetTimeToLiveAsync("key");
            Assert.That(ttl.Value.TotalSeconds, Is.GreaterThanOrEqualTo(9));
            await Task.Delay(1700);

            ttl = await RedisAsync.GetTimeToLiveAsync("key");
            Assert.That(ttl.Value.TotalSeconds, Is.LessThanOrEqualTo(9));
        }

        [Test]
        public async Task Can_GetServerTime()
        {
            var now = await RedisAsync.GetServerTimeAsync();

            now.Kind.PrintDump();
            now.ToString("D").Print();
            now.ToString("T").Print();

            "UtcNow".Print();
            DateTime.UtcNow.ToString("D").Print();
            DateTime.UtcNow.ToString("T").Print();

            Assert.That(now.Date, Is.EqualTo(DateTime.UtcNow.Date));
        }

        [Test]
        public async Task Can_Ping()
        {
            Assert.That(await RedisAsync.PingAsync(), Is.True);
        }

        [Test]
        public async Task Can_Echo()
        {
            Assert.That(await RedisAsync.EchoAsync("Hello"), Is.EqualTo("Hello"));
        }

        [Test]
        public async Task Can_SlaveOfNoOne()
        {
            await NativeAsync.SlaveOfNoOneAsync();
        }

        [Test]
        public async Task Can_Save()
        {
            try
            {
                await NativeAsync.SaveAsync();
            }
            catch (RedisResponseException e)
            {
                // if exception has that message then it still proves that BgSave works as expected.
                if (e.Message.StartsWith("Can't BGSAVE while AOF log rewriting is in progress")
                    || e.Message.StartsWith("An AOF log rewriting in progress: can't BGSAVE right now")
                    || e.Message.StartsWith("Background save already in progress")
                    || e.Message.StartsWith("Another child process is active (AOF?): can't BGSAVE right now"))
                    return;

                throw;
            }
        }

        [Test]
        public async Task Can_BgSave()
        {
            try
            {
                await NativeAsync.BgSaveAsync();
            }
            catch (RedisResponseException e)
            {
                // if exception has that message then it still proves that BgSave works as expected.
                if (e.Message.StartsWith("Can't BGSAVE while AOF log rewriting is in progress")
                    || e.Message.StartsWith("An AOF log rewriting in progress: can't BGSAVE right now")
                    || e.Message.StartsWith("Background save already in progress")
                    || e.Message.StartsWith("Another child process is active (AOF?): can't BGSAVE right now"))
                    return;

                throw;
            }
        }

        [Test]
        public async Task Can_Quit()
        {
            await NativeAsync.QuitAsync();
            RedisRaw.NamespacePrefix = null;
            CleanMask = null;
        }

        [Test]
        public async Task Can_BgRewriteAof()
        {
            await NativeAsync.BgRewriteAofAsync();
        }

        [Test]
        [Ignore("Works too well and shutdown the server")]
        public async Task Can_Shutdown()
        {
            await RedisAsync.ShutdownAsync();
        }

        [Test]
        public async Task Can_get_Keys_with_pattern()
        {
            for (int i = 0; i < 5; i++)
                await RedisAsync.SetValueAsync("k1:" + i, "val");
            for (int i = 0; i < 5; i++)
                await RedisAsync.SetValueAsync("k2:" + i, "val");

            var keys = await NativeAsync.KeysAsync("k1:*");
            Assert.That(keys.Length, Is.EqualTo(5));

            var scanKeys = await RedisAsync.SearchKeysAsync("k1:*");
            Assert.That(scanKeys.Count, Is.EqualTo(5));
        }

        [Test]
        public async Task Can_GetAll()
        {
            var keysMap = new Dictionary<string, string>();

            10.Times(i => keysMap.Add("key" + i, "val" + i));

            await RedisAsync.SetAllAsync(keysMap);

            var map = await RedisAsync.GetAllAsync<string>(keysMap.Keys);
            var mapKeys = await RedisAsync.GetValuesAsync(keysMap.Keys.ToList<string>());

            foreach (var entry in keysMap)
            {
                Assert.That(map.ContainsKey(entry.Key), Is.True);
                Assert.That(mapKeys.Contains(entry.Value), Is.True);
            }
        }

        [Test]
        public async Task Can_GetValues_JSON_strings()
        {
            var val = "{\"AuthorId\":0,\"Created\":\"\\/Date(1345961754013)\\/\",\"Name\":\"test\",\"Base64\":\"BQELAAEBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAViA/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP8BWAFYgP8BWAFYAViA/wFYAVgBWID/AVgBWAFYgP8BWAFYAViA/4D/gP+A/4D/AVgBWID/gP8BWID/gP8BWID/gP+A/wFYgP+A/4D/gP8BWID/gP+A/4D/gP+A/wFYAViA/4D/AViA/4D/AVgBWAFYgP8BWAFYAViA/4D/AViA/4D/gP+A/4D/gP8BWAFYgP+A/wFYgP+A/wFYgP+A/4D/gP+A/wFYgP+A/wFYgP+A/4D/gP+A/4D/AVgBWID/gP8BWID/gP8BWAFYAViA/wFYAVgBWID/gP8BWID/gP+A/4D/gP+A/wFYAViA/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP8BWAFYgP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/AVgBWID/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/wFYAViA/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP8BWAFYgP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/AVgBWID/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/4D/gP+A/wFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAFYAVgBWAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"}";

            await RedisAsync.SetValueAsync("UserLevel/1", val);

            var vals = await RedisAsync.GetValuesAsync(new List<string>(new[] { "UserLevel/1" }));

            Assert.That(vals.Count, Is.EqualTo(1));
            Assert.That(vals[0], Is.EqualTo(val));
        }

        [Test]
        public async Task Can_AcquireLock()
        {
            // guid here is to prevent competition between concurrent runtime tests
            var key = PrefixedKey("AcquireLockKeyTimeOut:" + Guid.NewGuid());
            var lockKey = PrefixedKey("Can_AcquireLock_TimeOut:" + Guid.NewGuid());
            await RedisAsync.IncrementValueAsync(key); //1

            Task[] tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                var snapsot = i;
                tasks[snapsot] = Task.Run(
                    () => IncrementKeyInsideLock(key, lockKey, snapsot, new RedisClient(TestConfig.SingleHost) { NamespacePrefix = RedisRaw.NamespacePrefix }.ForAsyncOnly())
                );
            }
            await Task.WhenAll(tasks);

            var val = await RedisAsync.GetAsync<int>(key);

            Assert.That(val, Is.EqualTo(1 + 5));
        }

        private async Task IncrementKeyInsideLock(String key, String lockKey, int clientNo, IRedisClientAsync client)
        {
            await using (await client.AcquireLockAsync(lockKey))
            {
                Debug.WriteLine(String.Format("client {0} acquired lock", clientNo));
                var val = await client.GetAsync<int>(key);

                await Task.Delay(200);

                await client.SetAsync(key, val + 1);
                Debug.WriteLine(String.Format("client {0} released lock", clientNo));
            }
        }

        [Test]
        public async Task Can_AcquireLock_TimeOut()
        {
            // guid here is to prevent competition between concurrent runtime tests
            var key = PrefixedKey("AcquireLockKeyTimeOut:" + Guid.NewGuid());
            var lockKey = PrefixedKey("Can_AcquireLock_TimeOut:" + Guid.NewGuid());
            await RedisAsync.IncrementValueAsync(key); //1
            _ = await RedisAsync.AcquireLockAsync(lockKey);
            var waitFor = TimeSpan.FromMilliseconds(1000);
            var now = DateTime.Now;

            try
            {
                await using var client = new RedisClient(TestConfig.SingleHost).ForAsyncOnly();
                await using (await client.AcquireLockAsync(lockKey, waitFor))
                {
                    await client.IncrementValueAsync(key); //2
                }
            }
            catch (TimeoutException)
            {
                var val = await RedisAsync.GetAsync<int>(key);
                Assert.That(val, Is.EqualTo(1));

                var timeTaken = DateTime.Now - now;
                Assert.That(timeTaken.TotalMilliseconds > waitFor.TotalMilliseconds, Is.True);
                Assert.That(timeTaken.TotalMilliseconds < waitFor.TotalMilliseconds + 1000, Is.True);
                return;
            }
            finally
            {
                await RedisAsync.RemoveAsync(key);
                await RedisAsync.RemoveAsync(lockKey);
            }
            Assert.Fail("should have Timed out");
        }

        [Test]
        public async Task Can_Append()
        {
            const string expectedString = "Hello, " + "World!";
            await RedisAsync.SetValueAsync("key", "Hello, ");
            var currentLength = await RedisAsync.AppendToValueAsync("key", "World!");

            Assert.That(currentLength, Is.EqualTo(expectedString.Length));

            var val = await RedisAsync.GetValueAsync("key");
            Assert.That(val, Is.EqualTo(expectedString));
        }

        [Test]
        public async Task Can_GetRange()
        {
            const string helloWorld = "Hello, World!";
            await RedisAsync.SetValueAsync("key", helloWorld);

            var fromIndex = "Hello, ".Length;
            var toIndex = "Hello, World".Length - 1;

            var expectedString = helloWorld.Substring(fromIndex, toIndex - fromIndex + 1);
            var world = await NativeAsync.GetRangeAsync("key", fromIndex, toIndex);

            Assert.That(world.Length, Is.EqualTo(expectedString.Length));
        }

        [Test]
        public async Task Can_create_distributed_lock()
        {
            var key = "lockkey";
            int lockTimeout = 2;

            var distributedLock = new DistributedLock().AsAsync();

            var state = await distributedLock.LockAsync(key, lockTimeout, lockTimeout, RedisAsync);
            Assert.AreEqual(state.Result, DistributedLock.LOCK_ACQUIRED);

            //can't re-lock
            distributedLock = new DistributedLock();
            state = await distributedLock.LockAsync(key, lockTimeout, lockTimeout, RedisAsync);
            Assert.AreEqual(state.Result, DistributedLock.LOCK_NOT_ACQUIRED);

            // re-acquire lock after timeout
            await Task.Delay(lockTimeout * 1000 + 1000);
            distributedLock = new DistributedLock();
            state = await distributedLock.LockAsync(key, lockTimeout, lockTimeout, RedisAsync);

            (var result, var expire) = state; // test decomposition since we are here
            Assert.AreEqual(result, DistributedLock.LOCK_RECOVERED);

            Assert.IsTrue(await distributedLock.UnlockAsync(key, expire, RedisAsync));

            //can now lock
            distributedLock = new DistributedLock();
            state = await distributedLock.LockAsync(key, lockTimeout, lockTimeout, RedisAsync);
            Assert.AreEqual(state.Result, DistributedLock.LOCK_ACQUIRED);

            //cleanup
            Assert.IsTrue(await distributedLock.UnlockAsync(key, state.Expiration, RedisAsync));
        }

        public class MyPoco
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public async Task Can_StoreObject()
        {
            object poco = new MyPoco { Id = 1, Name = "Test" };

            await RedisAsync.StoreObjectAsync(poco);

            Assert.That(await RedisAsync.GetValueAsync(RedisRaw.NamespacePrefix + "urn:mypoco:1"), Is.EqualTo("{\"Id\":1,\"Name\":\"Test\"}"));

            Assert.That(await RedisAsync.PopItemFromSetAsync(RedisRaw.NamespacePrefix + "ids:MyPoco"), Is.EqualTo("1"));
        }

        [Test]
        public async Task Can_store_multiple_keys()
        {
            var keys = 5.Times(x => "key" + x);
            var vals = 5.Times(x => "val" + x);

            using var redis = RedisClient.New();
            await RedisAsync.SetAllAsync(keys, vals);

            var all = await RedisAsync.GetValuesAsync(keys);
            Assert.AreEqual(vals, all);
        }

        [Test]
        public async Task Can_store_Dictionary()
        {
            var keys = 5.Times(x => "key" + x);
            var vals = 5.Times(x => "val" + x);
            var map = new Dictionary<string, string>();
            keys.ForEach(x => map[x] = "val" + x);

            await using var client = RedisClient.New().ForAsyncOnly();
            await client.SetAllAsync(map);

            var all = await client.GetValuesMapAsync(keys);
            Assert.AreEqual(map, all);
        }

        [Test]
        public async Task Can_store_Dictionary_as_objects()
        {
            var map = new Dictionary<string, object>
            {
                ["key_a"] = "123",
                ["key_b"] = null
            };

            await using var client = RedisClient.New().ForAsyncOnly();
            
            await client.SetAllAsync(map);

            Assert.That(await client.GetAsync<string>("key_a"), Is.EqualTo("123"));
            Assert.That(await client.GetValueAsync("key_b"), Is.EqualTo(""));
        }


        [Test]
        public async Task Can_store_Dictionary_as_bytes()
        {
            var map = new Dictionary<string, byte[]>
            {
                ["key_a"] = "123".ToUtf8Bytes(),
                ["key_b"] = null
            };

            await using var client = RedisClient.New().ForAsyncOnly();

            await client.SetAllAsync(map);

            Assert.That(await client.GetAsync<string>("key_a"), Is.EqualTo("123"));
            Assert.That(await client.GetValueAsync("key_b"), Is.EqualTo(""));
        }

        [Test]
        public async Task Should_reset_slowlog()
        {
            await using var client = RedisClient.New().ForAsyncOnly();
            await client.SlowlogResetAsync();
        }

        [Test]
        public async Task Can_get_slowlog()
        {
            await using var client = RedisClient.New().ForAsyncOnly();
            
            var log = await client.GetSlowlogAsync(10);

            foreach (var t in log)
            {
                Console.WriteLine(t.Id);
                Console.WriteLine(t.Duration);
                Console.WriteLine(t.Timestamp);
                Console.WriteLine(string.Join(":", t.Arguments));
            }
        }


        [Test]
        public async Task Can_change_db_at_runtime()
        {
            await using var redis = new RedisClient(TestConfig.SingleHost, TestConfig.RedisPort, db: 1).ForAsyncOnly();
            
            var val = Environment.TickCount;
            var key = "test" + val;
            try
            {
                await redis.SetAsync(key, val);
                await redis.SelectAsync(2);
                Assert.That(await redis.GetAsync<int>(key), Is.EqualTo(0));
                await redis.SelectAsync(1);
                Assert.That(await redis.GetAsync<int>(key), Is.EqualTo(val));
                await redis.DisposeAsync();
            }
            finally
            {
                await redis.SelectAsync(1);
                await redis.RemoveAsync(key);
            }
        }

        [Test]
        public async Task Can_Set_Expire_Seconds()
        {
            await RedisAsync.SetValueAsync("key", "val", expireIn: TimeSpan.FromSeconds(1));
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);
            await Task.Delay(2000);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);
        }

        [Test]
        public async Task Can_Set_Expire_MilliSeconds()
        {
            await RedisAsync.SetValueAsync("key", "val", expireIn: TimeSpan.FromMilliseconds(1000));
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);
            await Task.Delay(2000);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);
        }

        [Test]
        public async Task Can_Set_Expire_Seconds_if_exists()
        {
            Assert.That(await RedisAsync.SetValueIfExistsAsync("key", "val", expireIn: TimeSpan.FromMilliseconds(1500)),
                Is.False);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);

            await RedisAsync.SetValueAsync("key", "val");
            Assert.That(await RedisAsync.SetValueIfExistsAsync("key", "val", expireIn: TimeSpan.FromMilliseconds(1000)),
                Is.True);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);

            await Task.Delay(2000);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);
        }

        [Test]
        public async Task Can_Set_Expire_Seconds_if_not_exists()
        {
            Assert.That(await RedisAsync.SetValueIfNotExistsAsync("key", "val", expireIn: TimeSpan.FromMilliseconds(1000)),
                Is.True);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);

            Assert.That(await RedisAsync.SetValueIfNotExistsAsync("key", "val", expireIn: TimeSpan.FromMilliseconds(1000)),
                Is.False);

            await Task.Delay(2000);
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.False);

            await RedisAsync.RemoveAsync("key");
            await RedisAsync.SetValueIfNotExistsAsync("key", "val", expireIn: TimeSpan.FromMilliseconds(1000));
            Assert.That(await RedisAsync.ContainsKeyAsync("key"), Is.True);
        }
    }

}
