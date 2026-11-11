using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisExpiryRegressionTests
{
    // PEXPIRETIME needs Redis 7.0+. Older servers derive the deadline atomically from TIME + PTTL, which can differ
    // from the stored deadline by up to 1ms of rounding.
    static (long deadline, long tolerance) ExpireTimeMs(RedisClient redis, string key)
    {
        if (new Version(redis.Info["redis_version"]) >= new Version(7, 0))
            return (long.Parse(redis.Custom("PEXPIRETIME", key).Text), 0);
        var deadline = redis.ExecLuaAsInt(
            "local t = redis.call('TIME'); local p = redis.call('PTTL', KEYS[1]); if p < 0 then return p end; " +
            "return tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000) + p", new[] { key }, new string[0]);
        return (deadline, 1);
    }

    [TestCase(false, "Add")]
    [TestCase(false, "Replace")]
    [TestCase(true, "Add")]
    [TestCase(true, "Replace")]
    public async Task Relative_conditional_cache_writes_keep_the_requested_TTL(bool async, string operation)
    {
        await using var redis = new RedisClient(TestConfig.SingleHost);
        var key = "expiry-regression:" + Guid.NewGuid().ToString("N");
        try
        {
            if (operation == "Replace") redis.Set(key, "before");
            var ttl = TimeSpan.FromMinutes(1);
            var result = async
                ? operation == "Add"
                    ? await ((ICacheClientAsync)redis).AddAsync(key, "after", ttl)
                    : await ((ICacheClientAsync)redis).ReplaceAsync(key, "after", ttl)
                : operation == "Add"
                    ? redis.Add(key, "after", ttl)
                    : redis.Replace(key, "after", ttl);

            Assert.That(result, Is.True);
            Assert.That(redis.Get<string>(key), Is.EqualTo("after"));
            Assert.That(redis.PTtl(key), Is.InRange(50_000L, 60_000L));
        }
        finally
        {
            redis.Remove(key);
        }
    }

    [TestCase(false, "Set", false)]
    [TestCase(false, "Add", false)]
    [TestCase(false, "Replace", false)]
    [TestCase(true, "Set", false)]
    [TestCase(true, "Add", false)]
    [TestCase(true, "Replace", false)]
    [TestCase(false, "Set", true)]
    [TestCase(true, "Set", true)]
    public async Task Absolute_expiry_is_one_atomic_write_and_preserves_the_deadline(bool async, string operation,
        bool localTime)
    {
        await using var redis = new RedisClient(TestConfig.SingleHost);
        var key = "expiry-regression:" + Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddMinutes(1);
        if (localTime) expires = expires.ToLocalTime();
        var bytes = new byte[] { 0, 1, 128, 255 };
        try
        {
            if (operation == "Replace") redis.Set(key, bytes);
            var commands = new List<string>();
            using (Diagnostics.Redis.Subscribe(new CommandObserver(key, commands)))
                Assert.That(await Write(redis, async, operation, key, bytes, expires), Is.True);

            Assert.That(redis.Get<byte[]>(key), Is.EqualTo(bytes));
            var (deadline, tolerance) = ExpireTimeMs(redis, key);
            Assert.That(deadline, Is.EqualTo(expires.ToUnixTimeMs()).Within(tolerance));
            if (new Version(redis.Info["redis_version"]) >= new Version(6, 2))
            {
                Assert.That(commands, Has.Count.EqualTo(1), "Value and deadline must be sent as one server operation");
                Assert.That(commands[0], Is.AnyOf("SET", "EVAL"));
            }
            else
            {
                // Before Redis 6.2 a client's first SET PXAT is rejected with a syntax error (nothing is written),
                // then the value and deadline are applied by one atomic EVAL.
                Assert.That(commands.Last(), Is.EqualTo("EVAL"), "Value and deadline must be applied by one EVAL");
                Assert.That(commands.Take(commands.Count - 1), Is.All.EqualTo("SET"));
                Assert.That(commands, Has.Count.LessThanOrEqualTo(2));
            }
        }
        finally
        {
            redis.Remove(key);
        }
    }

    [TestCase(false, "Set")]
    [TestCase(false, "Add")]
    [TestCase(false, "Replace")]
    [TestCase(true, "Set")]
    [TestCase(true, "Add")]
    [TestCase(true, "Replace")]
    public async Task Past_absolute_expiry_removes_a_successfully_written_key(bool async, string operation)
    {
        await using var redis = new RedisClient(TestConfig.SingleHost);
        var key = "expiry-regression:" + Guid.NewGuid().ToString("N");
        try
        {
            if (operation == "Replace") redis.Set(key, new byte[] { 1 });
            Assert.That(await Write(redis, async, operation, key, [2], DateTime.UtcNow.AddDays(-1)),
                Is.True);
            Assert.That(redis.ContainsKey(key), Is.False);
        }
        finally
        {
            redis.Remove(key);
        }
    }

    [TestCase(false, "Set")]
    [TestCase(false, "Add")]
    [TestCase(false, "Replace")]
    [TestCase(true, "Set")]
    [TestCase(true, "Add")]
    [TestCase(true, "Replace")]
    public async Task Absolute_expiry_retains_future_TTL_on_older_servers(bool async, string operation)
    {
        await using var redis = new RedisClient(TestConfig.SingleHost);
        var key = "expiry-regression:" + Guid.NewGuid().ToString("N");
        try
        {
            if (operation == "Replace") redis.Set(key, new byte[] { 1 });
            Assert.That(await Write(redis, async, operation, key, [2], DateTime.UtcNow.AddMinutes(1)),
                Is.True);
            Assert.That(redis.Get<byte[]>(key), Is.EqualTo(new byte[] { 2 }));
            Assert.That(redis.PTtl(key), Is.InRange(50_000L, 60_000L));
            // A second operation on the same connection also exercises cached fallback selection.
            Assert.That(await Write(redis, async, "Add", key, [3], DateTime.UtcNow.AddDays(-1)), Is.False);
            Assert.That(redis.Get<byte[]>(key), Is.EqualTo(new byte[] { 2 }));
            Assert.That(redis.PTtl(key), Is.InRange(50_000L, 60_000L));
            redis.Remove(key);
            Assert.That(await Write(redis, async, "Replace", key, [4], DateTime.UtcNow.AddMinutes(1)),
                Is.False);
        }
        finally
        {
            redis.Remove(key);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Failed_condition_leaves_the_existing_value_and_expiration_untouched(bool async)
    {
        await using var redis = new RedisClient(TestConfig.SingleHost);
        var key = "expiry-regression:" + Guid.NewGuid().ToString("N");
        try
        {
            redis.Set(key, new byte[] { 1 }, TimeSpan.FromMinutes(2));
            var (before, tolerance) = ExpireTimeMs(redis, key);
            Assert.That(await Write(redis, async, "Add", key, [2], DateTime.UtcNow.AddDays(-1)), Is.False);
            Assert.That(redis.Get<byte[]>(key), Is.EqualTo(new byte[] { 1 }));
            Assert.That(ExpireTimeMs(redis, key).deadline, Is.EqualTo(before).Within(tolerance * 2));
            redis.Remove(key);
            Assert.That(await Write(redis, async, "Replace", key, [2], DateTime.UtcNow.AddMinutes(1)),
                Is.False);
            Assert.That(redis.ContainsKey(key), Is.False);
        }
        finally
        {
            redis.Remove(key);
        }
    }

    static Task<bool> Write(RedisClient redis, bool async, string operation, string key, byte[] value, DateTime expires)
    {
        if (async)
        {
            var cache = (ICacheClientAsync)redis;
            return operation == "Add" ? cache.AddAsync(key, value, expires)
                : operation == "Replace" ? cache.ReplaceAsync(key, value, expires)
                : cache.SetAsync(key, value, expires);
        }

        return Task.FromResult(operation == "Add" ? redis.Add(key, value, expires)
            : operation == "Replace" ? redis.Replace(key, value, expires) : redis.Set(key, value, expires));
    }

    sealed class CommandObserver(string key, List<string> commands) : IObserver<KeyValuePair<string, object>>
    {
        public void OnNext(KeyValuePair<string, object> item)
        {
            if (item is { Key: Diagnostics.Events.Redis.WriteCommandBefore, Value: RedisDiagnosticEvent e }
                && e.Command.Skip(1).Any(x =>
                    x.FromUtf8Bytes() == key))
            {
                commands.Add(e.Command[0].FromUtf8Bytes());
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}