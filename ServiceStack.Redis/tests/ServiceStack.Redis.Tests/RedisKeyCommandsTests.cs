using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisKeyCommandsTests
{
    RedisClient redis;
    string key;
    readonly byte[] bytes = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();

    [SetUp]
    public void SetUp()
    {
        redis = new RedisClient(TestConfig.SingleHost);
        key = "key-commands:" + Guid.NewGuid().ToString("N");
        if (Version.Parse(redis.Info["redis_version"]) < new Version(6, 2))
            Assert.Ignore("GETEX/GETDEL tests require Redis 6.2+");
    }

    [TearDown]
    public void TearDown()
    {
        redis.Del(key, key + ":other");
        redis.Dispose();
    }

    [TestCase(false, "EX")]
    [TestCase(true, "EX")]
    [TestCase(false, "PX")]
    [TestCase(true, "PX")]
    [TestCase(false, "EXAT")]
    [TestCase(true, "EXAT")]
    [TestCase(false, "PXAT")]
    [TestCase(true, "PXAT")]
    [TestCase(false, "PERSIST")]
    [TestCase(true, "PERSIST")]
    [TestCase(false, "NONE")]
    [TestCase(true, "NONE")]
    public async Task GetEx_preserves_binary_values_and_applies_selected_expiry(bool async, string option)
    {
        redis.Set(key, bytes, 0, 120_000);
        var deadline = DateTime.UtcNow.AddMinutes(1).ToUnixTimeMs();
        var options = option switch
        {
            "EX" => new RedisGetExOptions { ExpirySeconds = 60 },
            "PX" => new RedisGetExOptions { ExpiryMilliseconds = 60_000 },
            "EXAT" => new RedisGetExOptions { ExpiryAtSeconds = deadline / 1000 },
            "PXAT" => new RedisGetExOptions { ExpiryAtMilliseconds = deadline },
            "PERSIST" => new RedisGetExOptions { Persist = true },
            _ => null,
        };
        Assert.That(await GetEx(async, options), Is.EqualTo(bytes));
        Assert.That(redis.PTtl(key), option == "PERSIST" ? Is.EqualTo(-1L)
            : option == "NONE" ? Is.InRange(110_000L, 120_000L) : Is.InRange(50_000L, 60_000L));
        Assert.That(redis.Get(key), Is.EqualTo(bytes));
        redis.Del(key);
        Assert.That(await GetEx(async, options), Is.Null);
        Assert.That(redis.Exists(key), Is.Zero);
    }

    Task<byte[]> GetEx(bool async, RedisGetExOptions options) => async
        ? ((IRedisNativeClientAsync)redis).GetExAsync(key, options).AsTask()
        : Task.FromResult(((IRedisNativeClient)redis).GetEx(key, options));

    public class Payload
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Key_commands_preserve_existing_POCO_serialization(bool async)
    {
        redis.Set(key, new Payload { Name = "hello 👋", Count = 42 });
        var stored = redis.Get(key);
        Assert.That(await GetEx(async, new RedisGetExOptions { ExpiryMilliseconds = 60_000 }), Is.EqualTo(stored));
        Assert.That(redis.Get<Payload>(key).Name, Is.EqualTo("hello 👋"));
        var deleted = async ? await redis.GetDelAsync(key) : redis.GetDel(key);
        Assert.That(deleted, Is.EqualTo(stored));
        Assert.That(deleted.FromUtf8Bytes().FromJson<Payload>().Count, Is.EqualTo(42));
        Assert.That(redis.Exists(key), Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Past_GetEx_deadline_returns_value_then_expires_the_key(bool async)
    {
        redis.Set(key, bytes);
        Assert.That(
            await GetEx(async,
                new RedisGetExOptions { ExpiryAtMilliseconds = DateTime.UtcNow.AddMinutes(-1).ToUnixTimeMs() }),
            Is.EqualTo(bytes));
        Assert.That(redis.Exists(key), Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetDel_returns_all_bytes_then_missing_including_empty_values(bool async)
    {
        foreach (var value in new[] { bytes, Array.Empty<byte>() })
        {
            redis.Set(key, value);
            Assert.That(await GetDel(), Is.EqualTo(value));
            Assert.That(await GetDel(), Is.Null);
            Assert.That(redis.Exists(key), Is.Zero);
        }

        Task<byte[]> GetDel() => async
            ? ((IRedisNativeClientAsync)redis).GetDelAsync(key).AsTask()
            : Task.FromResult(((IRedisNativeClient)redis).GetDel(key));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Wrong_type_is_reported_without_deleting_the_key(bool async, bool delete)
    {
        redis.AddItemToList(key, "item");
        try
        {
            if (delete)
            {
                if (async) await redis.GetDelAsync(key);
                else redis.GetDel(key);
            }
            else await GetEx(async, new RedisGetExOptions { Persist = true });

            Assert.Fail("Expected WRONGTYPE");
        }
        catch (RedisResponseException e)
        {
            Assert.That(e.Message, Does.Contain("WRONGTYPE"));
        }

        Assert.That(redis.GetAllItemsFromList(key), Is.EqualTo(new[] { "item" }));
        Assert.That(redis.Ping(), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Unlink_counts_removed_keys_once_and_accepts_any_value_type(bool async)
    {
        redis.SetValue(key, "text");
        redis.AddItemToList(key + ":other", "item");
        var keys = new[] { key, key, key + ":other", key + ":missing" };
        var removed = async
            ? await ((IRedisNativeClientAsync)redis).UnlinkAsync(keys)
            : ((IRedisNativeClient)redis).Unlink(keys);
        Assert.That(removed, Is.EqualTo(2L));
        Assert.That(redis.Exists(key), Is.Zero);
        Assert.That(redis.Exists(key + ":other"), Is.Zero);
        Assert.That(async ? await redis.AsAsync().UnlinkAsync(key) : ((IRedisClient)redis).Unlink(key), Is.Zero);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Rich_commands_work_in_pipelines_and_transactions(bool async, bool transaction)
    {
        const string value = "hello 👋";
        redis.SetValue(key, value);
        redis.SetValue(key + ":other", "other");
        string refreshed = null, deleted = null;
        long removed = -1;
        var options = new RedisGetExOptions { ExpirySeconds = 60 };
        if (async)
        {
            var client = redis.AsAsync();
            if (transaction)
            {
                await using var batch = await client.CreateTransactionAsync();
                batch.QueueCommand(r => r.GetAndExpireValueAsync(key, options), x => refreshed = x);
                batch.QueueCommand(r => r.GetAndDeleteValueAsync(key), x => deleted = x);
                batch.QueueCommand(r => r.UnlinkAsync(key + ":other"), x => removed = x);
                Assert.That(await batch.CommitAsync(), Is.True);
            }
            else
            {
                await using var batch = client.CreatePipeline();
                batch.QueueCommand(r => r.GetAndExpireValueAsync(key, options), x => refreshed = x);
                batch.QueueCommand(r => r.GetAndDeleteValueAsync(key), x => deleted = x);
                batch.QueueCommand(r => r.UnlinkAsync(key + ":other"), x => removed = x);
                await batch.FlushAsync();
            }
        }
        else if (transaction)
        {
            using var batch = redis.CreateTransaction();
            batch.QueueCommand(r => r.GetAndExpireValue(key, options), x => refreshed = x);
            batch.QueueCommand(r => r.GetAndDeleteValue(key), x => deleted = x);
            batch.QueueCommand(r => r.Unlink(key + ":other"), x => removed = x);
            Assert.That(batch.Commit(), Is.True);
        }
        else
        {
            using var batch = redis.CreatePipeline();
            batch.QueueCommand(r => r.GetAndExpireValue(key, options), x => refreshed = x);
            batch.QueueCommand(r => r.GetAndDeleteValue(key), x => deleted = x);
            batch.QueueCommand(r => r.Unlink(key + ":other"), x => removed = x);
            batch.Flush();
        }

        Assert.That(refreshed, Is.EqualTo(value));
        Assert.That(deleted, Is.EqualTo(value));
        Assert.That(removed, Is.EqualTo(1L));
        Assert.That(redis.Exists(key), Is.Zero);
    }

    [TestCase("GETEX")]
    [TestCase("GETDEL")]
    [TestCase("UNLINK")]
    public async Task Precancelled_commands_do_not_mutate_the_key(string command)
    {
        redis.Set(key, bytes, 0, 120_000);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try
        {
            if (command == "GETEX")
                await redis.AsAsync().GetAndExpireValueAsync(key, new RedisGetExOptions { Persist = true }, cts.Token);
            else if (command == "GETDEL") await redis.AsAsync().GetAndDeleteValueAsync(key, cts.Token);
            else await redis.AsAsync().UnlinkAsync(key, cts.Token);
            Assert.Fail("Expected cancellation");
        }
        catch (OperationCanceledException)
        {
        }

        Assert.That(redis.Get(key), Is.EqualTo(bytes));
        Assert.That(redis.PTtl(key), Is.GreaterThan(110_000L));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task ACL_denials_are_propagated_and_allowed_commands_need_no_scripts(bool async, bool denyGetEx)
    {
        var user = "keycommands" + Guid.NewGuid().ToString("N");
        redis.SetValue(key, "hello");
        redis.Custom("ACL", "SETUSER", user, "on", ">test-password", "~" + key + "*", "+@all", "-eval", "-evalsha",
            denyGetEx ? "-getex" : "+getex");
        try
        {
            var endpoint = TestConfig.SingleHost.ToRedisEndpoint();
            endpoint.Username = user;
            endpoint.Password = "test-password";
            using var restricted = new RedisClient(endpoint);
            try
            {
                var result = async
                    ? await restricted.AsAsync()
                        .GetAndExpireValueAsync(key, new RedisGetExOptions { ExpirySeconds = 60 })
                    : ((IRedisClient)restricted).GetAndExpireValue(key, new RedisGetExOptions { ExpirySeconds = 60 });
                Assert.That(denyGetEx, Is.False, "Expected ACL denial");
                Assert.That(result, Is.EqualTo("hello"));
            }
            catch (RedisResponseException e)
            {
                Assert.That(denyGetEx, Is.True);
                Assert.That(e.Message, Does.Contain("NOPERM").And.Contain("getex"));
                Assert.That(redis.GetValue(key), Is.EqualTo("hello"));
                Assert.That(redis.PTtl(key), Is.EqualTo(-1L));
            }

            if (!denyGetEx)
            {
                Assert.That(
                    async
                        ? await restricted.AsAsync().GetAndDeleteValueAsync(key)
                        : ((IRedisClient)restricted).GetAndDeleteValue(key), Is.EqualTo("hello"));
                redis.SetValue(key, "hello");
                Assert.That(async ? await restricted.AsAsync().UnlinkAsync(key) : restricted.Unlink(key),
                    Is.EqualTo(1L));
            }
        }
        finally
        {
            redis.Custom("ACL", "DELUSER", user);
        }
    }

    [Test]
    public void Invalid_options_and_keys_are_rejected_before_sending()
    {
        redis.SetValue(key, "hello");
        Assert.Throws<ArgumentNullException>(() => redis.GetEx(null));
        Assert.Throws<ArgumentNullException>(() => redis.GetDel(null));
        Assert.Throws<ArgumentException>(() =>
            redis.GetEx(key, new RedisGetExOptions { Persist = true, ExpirySeconds = 1 }));
        Assert.Throws<ArgumentException>(() =>
            redis.GetEx(key, new RedisGetExOptions { ExpiryAtSeconds = 1, ExpiryMilliseconds = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => redis.GetEx(key, new RedisGetExOptions { ExpirySeconds = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            redis.GetEx(key, new RedisGetExOptions { ExpiryMilliseconds = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            redis.GetEx(key, new RedisGetExOptions { ExpiryAtMilliseconds = 0 }));
        Assert.Throws<ArgumentException>(() => redis.Unlink(Array.Empty<string>()));
        Assert.Throws<ArgumentNullException>(() => redis.Unlink(null));
        Assert.Throws<ArgumentException>(() => redis.Unlink(key, null));
        Assert.That(redis.GetValue(key), Is.EqualTo("hello"));
    }
}

[TestFixture, NonParallelizable]
public class RedisKeyCommandsLegacyTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Older_servers_reject_unsupported_commands_without_nonatomic_fallback(bool async)
    {
        using var redis = new RedisClient(TestConfig.SingleHost);
        if (redis.Info["redis_version"].Split('.').Take(2).Select(int.Parse).ToArray() is var version
            && (version[0] > 6 || version[0] == 6 && version[1] >= 2))
            Assert.Ignore("This case requires a Redis server older than 6.2");
        var key = "legacy-key-commands:" + Guid.NewGuid().ToString("N");
        try
        {
            redis.SetValue(key, "hello");
            foreach (var command in new[] { "GETEX", "GETDEL" })
            {
                try
                {
                    if (command == "GETEX")
                    {
                        if (async) await redis.GetExAsync(key, new RedisGetExOptions { Persist = true });
                        else redis.GetEx(key, new RedisGetExOptions { Persist = true });
                    }
                    else if (async) await redis.GetDelAsync(key);
                    else redis.GetDel(key);

                    Assert.Fail("Expected unknown command");
                }
                catch (RedisResponseException e)
                {
                    Assert.That(e.Message, Does.Contain("unknown command"));
                }

                Assert.That(redis.GetValue(key), Is.EqualTo("hello"));
            }

            Assert.That(async ? await redis.UnlinkAsync(key) : redis.Unlink(key), Is.EqualTo(1L));
        }
        finally
        {
            redis.Del(key);
        }
    }
}