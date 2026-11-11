using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisHashExpiryTests
{
    RedisClient redis;
    string key;
    static byte[] Bytes(string value) => value.ToUtf8Bytes();
    [SetUp] public void Before()
    {
        redis = new RedisClient(TestConfig.SingleHost); key = "hash-expiry:" + Guid.NewGuid().ToString("N");
        var info = redis.RawCommand("COMMAND", "INFO", "HEXPIRE");
        if (info.Children[0].IsNull) Assert.Ignore("Native hash-field expiry is not supported by this server.");
    }
    [TearDown] public void After() { redis.Del(key); redis.Dispose(); }
    void Put(string field, string value = "value") => redis.HSet(key, Bytes(field), Bytes(value));
    Task<long[]> Expire(bool async, int unit, long amount, string[] fields,
        RedisHashExpiryCondition condition = RedisHashExpiryCondition.None) => (async, unit) switch
    {
        (false, 0) => Task.FromResult(((IRedisClient)redis).HExpire(key, amount, fields, condition)),
        (false, 1) => Task.FromResult(((IRedisClient)redis).HPExpire(key, amount, fields, condition)),
        (false, 2) => Task.FromResult(((IRedisClient)redis).HExpireAt(key, amount, fields, condition)),
        (false, 3) => Task.FromResult(((IRedisClient)redis).HPExpireAt(key, amount, fields, condition)),
        (true, 0) => ((IRedisClientAsync)redis).HExpireAsync(key, amount, fields, condition).AsTask(),
        (true, 1) => ((IRedisClientAsync)redis).HPExpireAsync(key, amount, fields, condition).AsTask(),
        (true, 2) => ((IRedisClientAsync)redis).HExpireAtAsync(key, amount, fields, condition).AsTask(),
        _ => ((IRedisClientAsync)redis).HPExpireAtAsync(key, amount, fields, condition).AsTask()
    };
    long ParentDeadline() => long.Parse(redis.RawCommand("PEXPIRETIME", key).Data.FromUtf8Bytes(), CultureInfo.InvariantCulture);

    [TestCase(false, 0)][TestCase(true, 0)][TestCase(false, 1)][TestCase(true, 1)]
    [TestCase(false, 2)][TestCase(true, 2)][TestCase(false, 3)][TestCase(true, 3)]
    public async Task Expiry_units_preserve_per_field_order_and_parent_deadline(bool async, int unit)
    {
        Put("a"); Put("b"); Put("persistent"); redis.PExpire(key, 300000);
        var parent = ParentDeadline(); var fields = new[] { "b", "missing", "a" };
        var amount = unit switch { 0 => 60L, 1 => 60000L, 2 => DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60, _ => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 60000 };
        Assert.That(await Expire(async, unit, amount, fields), Is.EqualTo(new long[] { 1, -2, 1 }));
        var ttl = async ? await ((IRedisClientAsync)redis).HPTtlAsync(key, fields) : ((IRedisClient)redis).HPTtl(key, fields);
        Assert.That(ttl[0], Is.InRange(50000, 60000)); Assert.That(ttl[1], Is.EqualTo(-2)); Assert.That(ttl[2], Is.InRange(50000, 60000));
        var seconds = async ? await ((IRedisClientAsync)redis).HTtlAsync(key, fields) : ((IRedisClient)redis).HTtl(key, fields);
        Assert.That(seconds[0], Is.InRange(50, 60)); Assert.That(seconds[1], Is.EqualTo(-2));
        var times = async ? await ((IRedisClientAsync)redis).HPExpireTimeAsync(key, fields) : ((IRedisClient)redis).HPExpireTime(key, fields);
        var timesSeconds = async ? await ((IRedisClientAsync)redis).HExpireTimeAsync(key, fields) : ((IRedisClient)redis).HExpireTime(key, fields);
        Assert.That(times[1], Is.EqualTo(-2)); Assert.That(timesSeconds[1], Is.EqualTo(-2));
        if (unit == 2) Assert.That(times[0], Is.EqualTo(amount * 1000));
        if (unit == 3) Assert.That(times[0], Is.EqualTo(amount));
        Assert.That(timesSeconds[0], Is.InRange(times[0] / 1000, times[0] / 1000 + 1));
        Assert.That(ParentDeadline(), Is.EqualTo(parent));
        Assert.That(redis.HPTtl(Bytes(key), new[] { Bytes("persistent") }), Is.EqualTo(new long[] { -1 }));
    }

    [TestCase(false, RedisHashExpiryCondition.NX)][TestCase(true, RedisHashExpiryCondition.NX)]
    [TestCase(false, RedisHashExpiryCondition.XX)][TestCase(true, RedisHashExpiryCondition.XX)]
    [TestCase(false, RedisHashExpiryCondition.GT)][TestCase(true, RedisHashExpiryCondition.GT)]
    [TestCase(false, RedisHashExpiryCondition.LT)][TestCase(true, RedisHashExpiryCondition.LT)]
    public async Task Conditional_updates_preserve_success_and_rejection_for_each_field(bool async, RedisHashExpiryCondition condition)
    {
        Put("volatile"); Put("persistent"); var fields = new[] { "volatile", "persistent", "missing" };
        ((IRedisClient)redis).HPExpire(key, 100000, new[] { "volatile" });
        var before = ((IRedisClient)redis).HPExpireTime(key, fields);
        var expected = condition switch
        {
            RedisHashExpiryCondition.NX => new long[] { 0, 1, -2 },
            RedisHashExpiryCondition.XX => new long[] { 1, 0, -2 },
            RedisHashExpiryCondition.GT => new long[] { 0, 0, -2 },
            _ => new long[] { 1, 1, -2 }
        };
        Assert.That(await Expire(async, 1, 60000, fields, condition), Is.EqualTo(expected));
        var after = ((IRedisClient)redis).HPExpireTime(key, fields);
        for (var i = 0; i < fields.Length; i++) if (expected[i] == 0) Assert.That(after[i], Is.EqualTo(before[i]));
        if (condition == RedisHashExpiryCondition.GT)
            Assert.That(await Expire(async, 1, 200000, fields, condition), Is.EqualTo(new long[] { 1, 0, -2 }));
    }

    [TestCase(false, 0)][TestCase(true, 0)][TestCase(false, 1)][TestCase(true, 1)]
    [TestCase(false, 2)][TestCase(true, 2)][TestCase(false, 3)][TestCase(true, 3)]
    public async Task Zero_or_past_deadlines_delete_fields_without_deleting_other_fields(bool async, int unit)
    {
        Put("expire"); Put("keep");
        Assert.That(await Expire(async, unit, unit < 2 ? 0 : 1, new[] { "expire", "missing" }), Is.EqualTo(new long[] { 2, -2 }));
        Assert.That(redis.HGet(key, Bytes("expire")), Is.Null); Assert.That(redis.HGet(key, Bytes("keep")), Is.EqualTo(Bytes("value")));
        Put("conditional"); ((IRedisClient)redis).HPExpire(key, 60000, new[] { "conditional" });
        Assert.That(await Expire(async, unit, 0, new[] { "conditional" }, RedisHashExpiryCondition.NX), Is.EqualTo(new long[] { 0 }));
        Assert.That(redis.HGet(key, Bytes("conditional")), Is.Not.Null);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Persist_and_missing_hash_results_remain_distinct(bool async)
    {
        Put("volatile"); Put("persistent"); var fields = new[] { "volatile", "persistent", "missing" };
        await Expire(async, 1, 60000, new[] { "volatile" });
        var result = async ? await ((IRedisClientAsync)redis).HPersistAsync(key, fields) : ((IRedisClient)redis).HPersist(key, fields);
        Assert.That(result, Is.EqualTo(new long[] { 1, -1, -2 }));
        Assert.That(((IRedisClient)redis).HPTtl(key, fields), Is.EqualTo(new long[] { -1, -1, -2 }));
        Assert.That(((IRedisClient)redis).HExpireTime(key, fields), Is.EqualTo(new long[] { -1, -1, -2 }));
        redis.Del(key);
        Assert.That(await Expire(async, 1, 60000, fields), Is.EqualTo(new long[] { -2, -2, -2 }));
        Assert.That(((IRedisClient)redis).HTtl(key, fields), Is.EqualTo(new long[] { -2, -2, -2 }));
        Assert.That(((IRedisClient)redis).HPExpireTime(key, fields), Is.EqualTo(new long[] { -2, -2, -2 }));
        Assert.That(((IRedisClient)redis).HPersist(key, fields), Is.EqualTo(new long[] { -2, -2, -2 }));
    }

    [TestCase(false)][TestCase(true)]
    public async Task Binary_keys_fields_empty_names_and_payloads_round_trip(bool async)
    {
        var binaryKey = Bytes(key).Concat(new byte[] { 0, 255, 128 }).ToArray();
        var field = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray(); var empty = Array.Empty<byte>();
        redis.HSet(binaryKey, field, field); redis.HSet(binaryKey, empty, empty);
        var fields = new[] { field, empty, Bytes("missing") };
        var result = async ? await ((IRedisNativeClientAsync)redis).HPExpireAsync(binaryKey, 60000, fields)
            : ((IRedisNativeClient)redis).HPExpire(binaryKey, 60000, fields);
        Assert.That(result, Is.EqualTo(new long[] { 1, 1, -2 }));
        Assert.That(redis.HGet(binaryKey, field), Is.EqualTo(field)); Assert.That(redis.HGet(binaryKey, empty), Is.Empty);
        redis.RawCommand("DEL", binaryKey);
        Put("名前😀\0");
        Assert.That(await Expire(async, 0, 60, new[] { "名前😀\0" }), Is.EqualTo(new long[] { 1 }));
    }

    [TestCase(false)][TestCase(true)]
    public async Task Actual_expiration_leaves_other_fields_alive(bool async)
    {
        Put("short"); Put("keep");
        await Expire(async, 1, 50, new[] { "short" }); await Task.Delay(100);
        Assert.That(redis.HGet(key, Bytes("short")), Is.Null); Assert.That(redis.HGet(key, Bytes("keep")), Is.Not.Null);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Duplicate_fields_preserve_occurrence_order(bool async)
    {
        Put("a");
        Assert.That(await Expire(async, 1, 60000, new[] { "a", "a" }, RedisHashExpiryCondition.NX), Is.EqualTo(new long[] { 1, 0 }));
        Assert.That(((IRedisClient)redis).HPersist(key, new[] { "a", "a" }), Is.EqualTo(new long[] { 1, -1 }));
    }

    [Test]
    public void Replacing_a_field_clears_its_expiry_while_increment_preserves_it()
    {
        Put("replace"); Put("counter", "1"); ((IRedisClient)redis).HPExpire(key, 60000, new[] { "replace", "counter" });
        Put("replace", "new"); redis.IncrementValueInHash(key, "counter", 1);
        var ttl = ((IRedisClient)redis).HPTtl(key, new[] { "replace", "counter" });
        Assert.That(ttl[0], Is.EqualTo(-1)); Assert.That(ttl[1], Is.GreaterThan(0));
    }

    [TestCase(false)][TestCase(true)]
    public async Task Restricted_ACL_uses_native_commands_without_scripts_and_propagates_denials(bool async)
    {
        var user = "hfe:" + Guid.NewGuid().ToString("N");
        redis.Custom("ACL", "SETUSER", user, "on", ">secret", "~" + key + "*", "+@all", "-eval", "-evalsha");
        var endpoint = TestConfig.SingleHost.ToRedisEndpoint(); endpoint.Username = user; endpoint.Password = "secret";
        using var client = new RedisClient(endpoint);
        try
        {
            Put("a"); await client.ConnectAsync(); var writes = 0; client.OnBeforeFlush = () => writes++;
            var result = async ? await ((IRedisClientAsync)client).HPExpireAsync(key, 60000, new[] { "a" })
                : ((IRedisClient)client).HPExpire(key, 60000, new[] { "a" });
            Assert.That(result, Is.EqualTo(new long[] { 1 })); Assert.That(writes, Is.EqualTo(1));
            var before = ((IRedisClient)redis).HPExpireTime(key, new[] { "a" });
            redis.Custom("ACL", "SETUSER", user, "-hpexpire");
            try
            {
                if (async) await ((IRedisClientAsync)client).HPExpireAsync(key, 120000, new[] { "a" });
                else ((IRedisClient)client).HPExpire(key, 120000, new[] { "a" });
                Assert.Fail("Expected ACL rejection");
            }
            catch (RedisResponseException ex) { Assert.That(ex.Message, Does.Contain("NOPERM")); }
            Assert.That(((IRedisClient)redis).HPExpireTime(key, new[] { "a" }), Is.EqualTo(before));
        }
        finally { redis.Custom("ACL", "DELUSER", user); }
    }

    [TestCase(false)][TestCase(true)]
    public async Task Wrong_type_errors_do_not_mutate_or_misalign_the_connection(bool async)
    {
        redis.SetValue(key, "string");
        try { await Expire(async, 0, 10, new[] { "a" }); Assert.Fail(); }
        catch (RedisResponseException ex) { Assert.That(ex.Message, Does.Contain("WRONGTYPE")); }
        Assert.That(await ((IRedisClientAsync)redis).EchoAsync("aligned"), Is.EqualTo("aligned"));
        Assert.That(redis.GetValue(key), Is.EqualTo("string"));
    }

    [Test]
    public async Task Hash_expiry_round_trips_over_TLS()
    {
        var port = Environment.GetEnvironmentVariable("REDIS_TLS_PORT");
        var certPath = Environment.GetEnvironmentVariable("REDIS_TLS_CERT");
        if (port == null || certPath == null) Assert.Ignore("Set REDIS_TLS_PORT and REDIS_TLS_CERT for the TLS fixture");
#if NET10_0_OR_GREATER
        using var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(certPath);
#else
        using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);
#endif
        var previous = RedisConfig.CertificateValidationCallback;
        try
        {
            RedisConfig.CertificateValidationCallback = (_, actual, _, errors) =>
                (errors & (System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch | System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable)) == 0
                && actual.GetCertHashString() == cert.GetCertHashString();
            using var client = new RedisClient($"rediss://localhost:{port}?ConnectTimeout=2000");
            Put("a"); var fields = new[] { "a", "missing" };
            Assert.That(((IRedisClient)client).HPExpire(key, 60000, fields), Is.EqualTo(new long[] { 1, -2 }));
            var ttl = await ((IRedisClientAsync)client).HPTtlAsync(key, fields);
            Assert.That(ttl[0], Is.GreaterThan(0)); Assert.That(ttl[1], Is.EqualTo(-2));
            Assert.That(await ((IRedisClientAsync)client).HPersistAsync(key, fields), Is.EqualTo(new long[] { 1, -2 }));
        }
        finally { RedisConfig.CertificateValidationCallback = previous; }
    }

    [Test]
    public void Wire_numbers_are_invariant_under_nondefault_culture()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA"); Put("a");
            Assert.That(((IRedisClient)redis).HPExpire(key, 60000, new[] { "missing", "a" }), Is.EqualTo(new long[] { -2, 1 }));
            Assert.That(((IRedisClient)redis).HPersist(key, new[] { "missing", "a" }), Is.EqualTo(new long[] { -2, 1 }));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }
}

[TestFixture, NonParallelizable]
public class RedisHashExpiryCompatibilityTests
{
    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
    public async Task Unsupported_servers_do_not_emulate_or_mutate_hashes(int command)
    {
        using var client = new RedisClient(TestConfig.SingleHost);
        if (!client.RawCommand("COMMAND", "INFO", "HEXPIRE").Children[0].IsNull) Assert.Ignore("Requires a server without native hash expiry.");
        var key = "hfe-old:" + Guid.NewGuid().ToString("N");
        try
        {
            client.SetEntryInHash(key, "a", "unchanged"); client.PExpire(key, 120000);
            foreach (var async in new[] { false, true })
            {
                try { await Dispatch(client, key, command, async); Assert.Fail("Expected unknown command"); }
                catch (RedisResponseException ex) { Assert.That(ex.Message.ToLowerInvariant(), Does.Contain("unknown command")); }
                Assert.That(client.GetValueFromHash(key, "a"), Is.EqualTo("unchanged"));
                Assert.That(client.PTtl(key), Is.InRange(100000, 120000));
            }
        }
        finally { client.Del(key); }
    }
    static Task<long[]> Dispatch(RedisClient c, string key, int command, bool async)
    {
        var fields = new[] { "a" }; var a = (IRedisClientAsync)c; var s = (IRedisClient)c;
        if (async) return command switch
        {
            0 => a.HExpireAsync(key, 60, fields).AsTask(), 1 => a.HPExpireAsync(key, 60000, fields).AsTask(),
            2 => a.HExpireAtAsync(key, 2000000000, fields).AsTask(), 3 => a.HPExpireAtAsync(key, 2000000000000, fields).AsTask(),
            4 => a.HTtlAsync(key, fields).AsTask(), 5 => a.HPTtlAsync(key, fields).AsTask(),
            6 => a.HExpireTimeAsync(key, fields).AsTask(), 7 => a.HPExpireTimeAsync(key, fields).AsTask(), _ => a.HPersistAsync(key, fields).AsTask()
        };
        return Task.FromResult(command switch
        {
            0 => s.HExpire(key, 60, fields), 1 => s.HPExpire(key, 60000, fields),
            2 => s.HExpireAt(key, 2000000000, fields), 3 => s.HPExpireAt(key, 2000000000000, fields),
            4 => s.HTtl(key, fields), 5 => s.HPTtl(key, fields), 6 => s.HExpireTime(key, fields),
            7 => s.HPExpireTime(key, fields), _ => s.HPersist(key, fields)
        });
    }

    [Test]
    public async Task Invalid_arguments_precancellation_and_batching_are_rejected_locally()
    {
        using var client = new RedisClient(TestConfig.SingleHost); var fields = new[] { "a".ToUtf8Bytes() };
        Assert.Throws<ArgumentNullException>(() => client.HExpire((byte[])null, 1, fields));
        Assert.Throws<ArgumentNullException>(() => client.HExpire("key".ToUtf8Bytes(), 1, null));
        Assert.Throws<ArgumentException>(() => client.HExpire("key".ToUtf8Bytes(), 1, Array.Empty<byte[]>()));
        Assert.Throws<ArgumentException>(() => client.HExpire("key".ToUtf8Bytes(), 1, new byte[][] { null }));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.HExpire("key".ToUtf8Bytes(), -1, fields));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.HExpire("key".ToUtf8Bytes(), 1, fields, (RedisHashExpiryCondition)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.HExpire("key".ToUtf8Bytes(), 1, fields, RedisHashExpiryCondition.NX | RedisHashExpiryCondition.XX));
        Assert.Throws<ArgumentException>(() => ((IRedisClient)client).HTtl("key", new string[] { null }));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        try { await ((IRedisClientAsync)client).HPExpireAsync("key", 10, new[] { "a" }, token: cts.Token); Assert.Fail(); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(client.HasConnected, Is.False);
        await client.ConnectAsync();
        var writes = 0; client.OnBeforeFlush = () => writes++;
        using (var pipeline = client.CreatePipeline())
            Assert.Throws<NotSupportedException>(() => ((IRedisClient)client).HTtl("key", new[] { "a" }));
        Assert.That(writes, Is.Zero);
        client.OnBeforeFlush = null;
        await using (var pipeline = ((IRedisClientAsync)client).CreatePipeline())
            Assert.ThrowsAsync<NotSupportedException>(async () => await ((IRedisClientAsync)client).HPersistAsync("key", new[] { "a" }));
        using (var transaction = client.CreateTransaction())
            Assert.Throws<NotSupportedException>(() => ((IRedisClient)client).HExpire("key", 1, new[] { "a" }));
        Assert.That(await ((IRedisClientAsync)client).PingAsync(), Is.True);
    }
}
