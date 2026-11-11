using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisStreamsTests
{
    RedisClient redis;
    string key;

    static RedisStreamField[] Fields =>
    [
        new() { Name = [0, 255, 128], Value = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray() },
        new() { Name = "empty".ToUtf8Bytes(), Value = Array.Empty<byte>() }
    ];

    static RedisStreamPosition[] Position(string key, string id = "0-0") =>
        new[] { new RedisStreamPosition { Stream = key, Id = id } };

    [SetUp]
    public void Setup()
    {
        redis = new RedisClient(TestConfig.SingleHost);
        key = "streams:" + Guid.NewGuid().ToString("N");
    }

    [TearDown]
    public void Cleanup()
    {
        redis.Del(key, key + ":other");
        redis.Dispose();
    }

    void RequireXAutoClaim()
    {
        if (Version.Parse(redis.Info["redis_version"]) < new Version(6, 2))
            Assert.Ignore("XAUTOCLAIM requires Redis 6.2+");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Append_range_reverse_length_and_multistream_reads_are_binary_and_ID_safe(bool async)
    {
        IRedisNativeClient native = redis;
        IRedisNativeClientAsync nativeAsync = redis;
        const string id1 = "9007199254740993-1", id2 = "9007199254740993-2";
        Assert.That(async ? await nativeAsync.XAddAsync(key, Fields, id1) : native.XAdd(key, Fields, id1),
            Is.EqualTo(id1));
        Assert.That(async ? await nativeAsync.XAddAsync(key, Fields, id2) : native.XAdd(key, Fields, id2),
            Is.EqualTo(id2));
        Assert.That(async ? await nativeAsync.XLenAsync(key) : native.XLen(key), Is.EqualTo(2L));
        var range = async ? await nativeAsync.XRangeAsync(key) : native.XRange(key);
        Assert.That(range.Select(x => x.Id), Is.EqualTo(new[] { id1, id2 }));
        Assert.That(range[0].Fields[0].Name, Is.EqualTo(Fields[0].Name));
        Assert.That(range[0].Fields[0].Value, Is.EqualTo(Fields[0].Value));
        Assert.That(range[0].Fields[1].Value, Is.Empty);
        var reverse = async
            ? await redis.AsAsync().XRangeAsync(key, count: 1, reverse: true)
            : ((IRedisClient)redis).XRange(key, count: 1, reverse: true);
        Assert.That(reverse.Single().Id, Is.EqualTo(id2));
        redis.XAdd(key + ":other", Fields, "1-0");
        var positions = Position(key).Concat(Position(key + ":other")).ToArray();
        var read = async ? await nativeAsync.XReadAsync(positions, count: 1) : native.XRead(positions, count: 1);
        Assert.That(read.Select(x => x.Stream), Is.EqualTo(new[] { key, key + ":other" }));
        Assert.That(read[0].Entries.Single().Id, Is.EqualTo(id1));
        Assert.That(async ? await redis.XReadAsync(Position(key, id2)) : redis.XRead(Position(key, id2)), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Group_delivery_pending_claim_recovery_and_deleted_payloads_are_explicit(bool async)
    {
        RequireXAutoClaim();
        Assert.That(
            async
                ? await redis.XGroupCreateAsync(key, "group", makeStream: true)
                : redis.XGroupCreate(key, "group", makeStream: true), Is.True);
        var id1 = redis.XAdd(key, Fields);
        var id2 = redis.XAdd(key, Fields);
        var delivered = async
            ? await redis.AsAsync().XReadGroupAsync("group", "a", Position(key, ">"))
            : ((IRedisClient)redis).XReadGroup("group", "a", Position(key, ">"));
        Assert.That(delivered.Single().Entries.Select(x => x.Id), Is.EqualTo(new[] { id1, id2 }));
        var summary = async ? await redis.XPendingAsync(key, "group") : redis.XPending(key, "group");
        Assert.That(summary.Count, Is.EqualTo(2));
        Assert.That(summary.LowestId, Is.EqualTo(id1));
        Assert.That(summary.Consumers.Single().Count, Is.EqualTo(2));
        var claimed = async
            ? await redis.XClaimAsync(key, "group", "b", 0, new[] { id1 })
            : redis.XClaim(key, "group", "b", 0, new[] { id1 });
        Assert.That(claimed.Single().Id, Is.EqualTo(id1));
        var pending = async
            ? await redis.XPendingEntriesAsync(key, "group", consumer: "b")
            : redis.XPendingEntries(key, "group", consumer: "b");
        Assert.That(pending.Single().DeliveryCount, Is.EqualTo(2));
        Assert.That(pending[0].Consumer, Is.EqualTo("b"));
        var recovered =
            async ? await redis.XAutoClaimAsync(key, "group", "c", 0) : redis.XAutoClaim(key, "group", "c", 0);
        Assert.That(recovered.Entries.Select(x => x.Id), Is.EqualTo(new[] { id1, id2 }));
        Assert.That(recovered.NextStartId, Is.EqualTo("0-0"));
        Assert.That(
            async ? await redis.XAckAsync(key, "group", new[] { id1 }) : redis.XAck(key, "group", new[] { id1 }),
            Is.EqualTo(1));
        Assert.That(redis.XPending(key, "group").Count, Is.EqualTo(1));
        Assert.That(async ? await redis.XDelAsync(key, new[] { id2 }) : redis.XDel(key, new[] { id2 }), Is.EqualTo(1));
        var history = async
            ? await redis.XReadGroupAsync("group", "c", Position(key))
            : redis.XReadGroup("group", "c", Position(key));
        Assert.That(history.Single().Entries.Single().Id, Is.EqualTo(id2));
        Assert.That(history[0].Entries[0].Fields, Is.Null, "Deleted pending payload must not become an empty payload");
        var cleaned =
            async ? await redis.XAutoClaimAsync(key, "group", "d", 0) : redis.XAutoClaim(key, "group", "d", 0);
        if (int.Parse(redis.Info["redis_version"].Split('.')[0]) >= 7)
            Assert.That(cleaned.DeletedIds, Is.EqualTo(new[] { id2 }));
        Assert.That(async ? await redis.XGroupDestroyAsync(key, "group") : redis.XGroupDestroy(key, "group"),
            Is.EqualTo(1));
    }

    public class Event
    {
        public string Message { get; set; }
        public int Version { get; set; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Typed_facade_preserves_unknown_fields_nulls_and_requires_explicit_ack(bool async)
    {
        IRedisClient client = redis;
        var clientAsync = redis.AsAsync();
        var id = async
            ? await clientAsync.AddStreamEntryAsync(key, new Event { Message = "hello 👋", Version = 2 })
            : client.AddStreamEntry(key, new Event { Message = "hello 👋", Version = 2 });
        if (async) await clientAsync.AddStreamEntryAsync<Event>(key, null);
        else client.AddStreamEntry<Event>(key, null);
        redis.XAdd(key, new[] { new RedisStreamField { Name = "other".ToUtf8Bytes(), Value = new byte[] { 255 } } });
        var typed = async
            ? await clientAsync.ReadStreamEntriesAsync<Event>(Position(key))
            : client.ReadStreamEntries<Event>(Position(key));
        Assert.That(typed.Length, Is.EqualTo(3));
        Assert.That(typed[0].Id, Is.EqualTo(id));
        Assert.That(typed[0].Value.Message, Is.EqualTo("hello 👋"));
        Assert.That(typed[0].Value.Version, Is.EqualTo(2));
        Assert.That(typed[1].HasValue, Is.True);
        Assert.That(typed[1].Value, Is.Null);
        Assert.That(typed[2].HasValue, Is.False);
        Assert.That(typed[2].Fields[0].Value, Is.EqualTo(new byte[] { 255 }));
        redis.XGroupCreate(key, "g");
        var group = async
            ? await clientAsync.ReadStreamGroupEntriesAsync<Event>("g", "consumer", Position(key, ">"))
            : client.ReadStreamGroupEntries<Event>("g", "consumer", Position(key, ">"));
        Assert.That(group.Length, Is.EqualTo(3));
        Assert.That(redis.XPending(key, "g").Count, Is.EqualTo(3));
        redis.XAck(key, "g", group.Select(x => x.Id).ToArray());
        var empty = redis.XPending(key, "g");
        Assert.That(empty.Count, Is.Zero);
        Assert.That(empty.LowestId, Is.Null);
        Assert.That(empty.Consumers, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Retention_delete_and_timed_empty_read_have_correct_results(bool async)
    {
        for (var i = 0; i < 5; i++)
            if (async) await redis.XAddAsync(key, Fields, maxLength: 2);
            else redis.XAdd(key, Fields, maxLength: 2);
        Assert.That(redis.XLen(key), Is.EqualTo(2));
        Assert.That(async ? await redis.XTrimAsync(key, 1) : redis.XTrim(key, 1), Is.EqualTo(1));
        Assert.That(async ? await redis.XTrimAsync(key, 0) : redis.XTrim(key, 0), Is.EqualTo(1));
        Assert.That(redis.XLen(key), Is.Zero);
        Assert.That(
            async
                ? await redis.XReadAsync(Position(key), blockMilliseconds: 20)
                : redis.XRead(Position(key), blockMilliseconds: 20), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Nil_metadata_distinguishes_nil_arrays_from_empty_arrays_without_changing_old_shape(bool async)
    {
        var native = (IRedisNativeClientAsync)redis;
        var nil = async
            ? await native.RawCommandAsync(["BLPOP", key, "1"], CancellationToken.None)
            : redis.RawCommand("BLPOP", key, "1");
        Assert.That(nil.IsNull, Is.True);
        Assert.That(nil.Children, Is.Empty);
        var empty = async
            ? await native.RawCommandAsync(["EVAL", "return {}", 0], CancellationToken.None)
            : redis.RawCommand("EVAL", "return {}", 0);
        Assert.That(empty.IsNull, Is.False);
        Assert.That(empty.Children, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cancelled_blocking_reads_close_protocol_state_before_pool_reuse(bool group)
    {
        using var pool = new RedisManagerPool(TestConfig.SingleHost,
            new RedisPoolConfig { MaxPoolSize = 1, MaxLiveConnections = 1 });
        var manager = (IRedisClientsManagerAsync)pool;
        var held = await manager.GetClientAsync();
        var name = "stream-block-" + Guid.NewGuid().ToString("N");
        await held.SetClientAsync(name);
        if (group) await held.XGroupCreateAsync(key, "g", makeStream: true);
        using var cts = new CancellationTokenSource();
        var blocked = group
            ? held.XReadGroupAsync("g", "c", Position(key, ">"), blockMilliseconds: 0, token: cts.Token).AsTask()
            : held.XReadAsync(Position(key), blockMilliseconds: 0, token: cts.Token).AsTask();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!redis.Custom("CLIENT", "LIST").Text.Split('\n').Any(x =>
                   x.Contains("name=" + name + " ") && x.Contains(group ? "cmd=xreadgroup" : "cmd=xread")))
        {
            if (watch.ElapsedMilliseconds > 3000) Assert.Fail("Read never reached server blocking state");
            await Task.Delay(5);
        }

        cts.Cancel();
        Assert.That(await Task.WhenAny(blocked, Task.Delay(3000)), Is.SameAs(blocked));
        Assert.ThrowsAsync<OperationCanceledException>(async () => await blocked);
        await held.DisposeAsync();
        await using var next = await manager.GetClientAsync();
        Assert.That(await next.PingAsync(), Is.True);
        Assert.That(await next.EchoAsync("clean"), Is.EqualTo("clean"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ACL_and_group_errors_are_not_reinterpreted_or_emulated(bool async)
    {
        redis.XGroupCreate(key, "g", makeStream: true);
        Assert.Throws<RedisResponseException>(() => redis.XGroupCreate(key, "g"));
        var id = redis.XAdd(key, Fields);
        var user = "streams" + Guid.NewGuid().ToString("N");
        redis.Custom("ACL", "SETUSER", user, "on", ">test-password", "~" + key + "*", "+@all", "-xreadgroup", "-eval",
            "-evalsha");
        try
        {
            var endpoint = TestConfig.SingleHost.ToRedisEndpoint();
            endpoint.Username = user;
            endpoint.Password = "test-password";
            using var restricted = new RedisClient(endpoint);
            try
            {
                if (async) await restricted.XReadGroupAsync("g", "c", Position(key, ">"));
                else restricted.XReadGroup("g", "c", Position(key, ">"));
                Assert.Fail("Expected ACL error");
            }
            catch (RedisResponseException e)
            {
                Assert.That(e.Message, Does.Contain("NOPERM"));
            }

            Assert.That(redis.XPending(key, "g").Count, Is.Zero);
            Assert.That(async ? await restricted.XRangeAsync(key) : restricted.XRange(key), Has.Length.EqualTo(1));
        }
        finally
        {
            redis.Custom("ACL", "DELUSER", user);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Recovery_cursor_reclaims_unacknowledged_entries_after_reader_disconnect(bool async)
    {
        RequireXAutoClaim();
        redis.XGroupCreate(key, "g", makeStream: true);
        var ids = Enumerable.Range(0, 3).Select(_ => redis.XAdd(key, Fields)).ToArray();
        using (var stopped = new RedisClient(TestConfig.SingleHost))
            Assert.That(stopped.XReadGroup("g", "stopped", Position(key, ">")).Single().Entries.Length, Is.EqualTo(3));
        var recovered = new System.Collections.Generic.List<string>();
        var cursor = "0-0";
        var calls = 0;
        do
        {
            var page = async
                ? await redis.XAutoClaimAsync(key, "g", "replacement", 0, cursor, 1)
                : redis.XAutoClaim(key, "g", "replacement", 0, cursor, 1);
            recovered.AddRange(page.Entries.Select(x => x.Id));
            Assert.That(page.Entries.Length, Is.LessThanOrEqualTo(1));
            cursor = page.NextStartId;
            Assert.That(++calls, Is.LessThanOrEqualTo(4));
        } while (cursor != "0-0");

        Assert.That(recovered, Is.EqualTo(ids));
        Assert.That(redis.XPending(key, "g").Count, Is.EqualTo(3), "Reclaiming is not acknowledgment");
        redis.XAck(key, "g", ids);
        Assert.That(redis.XPending(key, "g").Count, Is.Zero);
    }

    [Test]
    public void Validation_cancellation_and_batch_context_reject_without_sending_mutations()
    {
        Assert.Throws<ArgumentException>(() => redis.XAdd(key, Array.Empty<RedisStreamField>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => redis.XTrim(key, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => redis.XRead(Position(key), count: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => redis.XRead(Position(key), blockMilliseconds: -1));
        Assert.Throws<ArgumentNullException>(() => redis.XReadGroup(null, "c", Position(key)));
        Assert.Throws<ArgumentException>(() => redis.XAck(key, "g", Array.Empty<string>()));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await redis.XAddAsync(key, Fields, token: cts.Token));
        Assert.That(redis.Exists(key), Is.Zero);
        using (var batch = redis.CreatePipeline()) Assert.Throws<NotSupportedException>(() => redis.XRange(key));
    }
}