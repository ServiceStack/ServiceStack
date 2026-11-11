using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisStreamBatchTests
{
    RedisClient redis, admin;
    string prefix, key;
    static RedisStreamField[] Fields(int i) => new[]
    {
        new RedisStreamField { Name = new byte[] { 0, 255 }, Value = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray() },
        new RedisStreamField { Name = "number".ToUtf8Bytes(), Value = i.ToString().ToUtf8Bytes() },
        new RedisStreamField { Name = "empty".ToUtf8Bytes(), Value = Array.Empty<byte>() }
    };
    [SetUp] public void Before()
    {
        redis = new RedisClient(TestConfig.SingleHost); admin = new RedisClient(TestConfig.SingleHost);
        prefix = "stream-batch:" + Guid.NewGuid().ToString("N") + ":"; key = prefix + "events";
    }
    [TearDown] public void After()
    {
        redis.Dispose(); admin.UnlinkByPattern(prefix + "*"); admin.Dispose();
    }
    Task<string[]> Append(bool async, IEnumerable<RedisStreamField[]> entries, int batchSize = 100,
        long? maxLength = null, CancellationToken token = default) => async
        ? ((IRedisClientAsync)redis).AddStreamEntriesAsync(key, entries, batchSize, maxLength, token: token).AsTask()
        : Task.FromResult(((IRedisClient)redis).AddStreamEntries(key, entries, batchSize, maxLength));

    [TestCase(false)][TestCase(true)]
    public async Task Primitive_commands_preserve_pipeline_order_and_results(bool async)
    {
        var old = redis.XAdd(key, Fields(0)); redis.XGroupCreate(key, "group");
        redis.XReadGroup("group", "consumer", new[] { new RedisStreamPosition { Stream = key, Id = ">" } });
        var results = new List<string>();
        var client = (IRedisClientAsync)redis;
        if (async)
        {
            await using var pipe = client.CreatePipeline();
            pipe.QueueCommand(r => r.XAddAsync(key, Fields(1)), id => results.Add(id));
            pipe.QueueCommand(r => r.XAckAsync(key, "group", new[] { old }), n => results.Add("ack:" + n));
            pipe.QueueCommand(r => r.XDelAsync(key, new[] { old }), n => results.Add("del:" + n));
            pipe.QueueCommand(r => r.XLenAsync(key), n => results.Add("len:" + n));
            pipe.QueueCommand(r => r.XTrimAsync(key, 0), n => results.Add("trim:" + n));
            pipe.QueueCommand(r => r.EchoAsync("aligned"), s => results.Add(s));
            Assert.That(admin.XLen(key), Is.EqualTo(1)); Assert.That(results, Is.Empty);
            await pipe.FlushAsync();
        }
        else
        {
            using var pipe = redis.CreatePipeline();
            pipe.QueueCommand(r => r.XAdd(key, Fields(1)), id => results.Add(id));
            pipe.QueueCommand(r => r.XAck(key, "group", new[] { old }), n => results.Add("ack:" + n));
            pipe.QueueCommand(r => r.XDel(key, new[] { old }), n => results.Add("del:" + n));
            pipe.QueueCommand(r => r.XLen(key), n => results.Add("len:" + n));
            pipe.QueueCommand(r => r.XTrim(key, 0), n => results.Add("trim:" + n));
            pipe.QueueCommand(r => r.Echo("aligned"), s => results.Add(s));
            Assert.That(admin.XLen(key), Is.EqualTo(1)); Assert.That(results, Is.Empty);
            pipe.Flush();
        }
        Assert.That(results[0], Does.Contain("-"));
        Assert.That(results.Skip(1), Is.EqualTo(new[] { "ack:1", "del:1", "len:1", "trim:1", "aligned" }));
        Assert.That(await client.PingAsync(), Is.True);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Binary_bulk_append_has_hard_batch_bound_and_ordered_ids(bool async)
    {
        await redis.ConnectAsync();
        var seen = new List<long>(); redis.OnBeforeFlush = () => seen.Add(admin.XLen(key));
        var ids = await Append(async, Enumerable.Range(0, 257).Select(Fields), 17);
        redis.OnBeforeFlush = null;
        Assert.That(seen, Is.EqualTo(Enumerable.Range(0, 16).Select(i => (long)i * 17)));
        var rows = admin.XRange(key);
        Assert.That(ids, Is.EqualTo(rows.Select(x => x.Id)));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(257));
        for (var i = 0; i < rows.Length; i++)
        {
            Assert.That(rows[i].Fields[0].Name, Is.EqualTo(Fields(i)[0].Name));
            Assert.That(rows[i].Fields[0].Value, Is.EqualTo(Fields(i)[0].Value));
            Assert.That(rows[i].Fields[1].Value.FromUtf8Bytes(), Is.EqualTo(i.ToString()));
            Assert.That(rows[i].Fields[2].Value, Is.Empty);
        }
    }

    [TestCase(false)][TestCase(true)]
    public async Task Typed_bulk_append_uses_existing_json_convention_and_retention(bool async)
    {
        var values = Enumerable.Range(0, 23).Select(i => i == 22 ? null : "value:" + i).ToArray();
        var ids = async
            ? await ((IRedisClientAsync)redis).AddStreamEntriesAsync(key, values, 5, maxLength: 7)
            : ((IRedisClient)redis).AddStreamEntries(key, values, 5, maxLength: 7);
        var entries = ((IRedisClient)admin).ReadStreamEntries<string>(new[] { new RedisStreamPosition { Stream = key } });
        Assert.That(ids.Length, Is.EqualTo(23)); Assert.That(entries.Select(x => x.Id), Is.EqualTo(ids.Skip(16)));
        Assert.That(entries.Select(x => x.Value), Is.EqualTo(values.Skip(16)));
        Assert.That(entries.All(x => x.HasValue), Is.True);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Empty_input_does_not_open_a_connection(bool async)
    {
        Assert.That(await Append(async, Array.Empty<RedisStreamField[]>()), Is.Empty);
        Assert.That(redis.HasConnected, Is.False);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Invalid_fields_are_rejected_before_sending_the_current_batch(bool async)
    {
        try { await Append(async, new[] { Fields(0), Array.Empty<RedisStreamField>() }, 2); Assert.Fail(); }
        catch (ArgumentException) { }
        Assert.That(redis.HasConnected, Is.False); Assert.That(admin.XLen(key), Is.Zero);
        try { await Append(async, new[] { Fields(0), Fields(1), Array.Empty<RedisStreamField>() }, 2); Assert.Fail(); }
        catch (ArgumentException) { }
        Assert.That(admin.XLen(key), Is.EqualTo(2)); // Completed batches are retained.
    }

    [TestCase(false)][TestCase(true)]
    public async Task Server_failure_stops_after_the_failed_batch_without_replay(bool async)
    {
        var user = "batch:" + Guid.NewGuid().ToString("N");
        admin.Custom("ACL", "SETUSER", user, "on", ">secret", "~" + prefix + "*", "+@all");
        redis.Dispose();
        var endpoint = TestConfig.SingleHost.ToRedisEndpoint(); endpoint.Username = user; endpoint.Password = "secret";
        redis = new RedisClient(endpoint);
        try
        {
            await redis.ConnectAsync();
            var flushes = 0;
            redis.OnBeforeFlush = () => { if (++flushes == 2) admin.Custom("ACL", "SETUSER", user, "-xadd"); };
            try { await Append(async, Enumerable.Range(0, 9).Select(Fields), 2); Assert.Fail(); }
            catch (RedisResponseException ex) { Assert.That(ex.Message, Does.Contain("NOPERM")); }
            redis.OnBeforeFlush = null;
            Assert.That(flushes, Is.EqualTo(2)); Assert.That(admin.XLen(key), Is.EqualTo(2));
            Assert.That(redis.HasConnected, Is.False);
            admin.Custom("ACL", "SETUSER", user, "+xadd");
            Assert.That(await ((IRedisClientAsync)redis).PingAsync(), Is.True);
        }
        finally { admin.Custom("ACL", "DELUSER", user); }
    }

    [TestCase(false)][TestCase(true)]
    public async Task Large_binary_batches_send_all_bytes_with_small_socket_buffers(bool async)
    {
        redis.SendTimeout = redis.ReceiveTimeout = 5000;
        await redis.ConnectAsync();
        var socket = (Socket)typeof(RedisNativeClient).GetField("socket", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(redis);
        socket.SendBufferSize = 1024;
        var data = Enumerable.Range(0, 512 * 1024).Select(i => (byte)i).ToArray();
        var fields = new[] { new RedisStreamField { Name = "blob".ToUtf8Bytes(), Value = data } };
        using var cts = new CancellationTokenSource(15000);
        var ids = await Append(async, Enumerable.Repeat(fields, 12), 4, token: cts.Token);
        Assert.That(ids.Length, Is.EqualTo(12));
        Assert.That(admin.XRange(key).All(x => x.Fields[0].Value.SequenceEqual(data)), Is.True);
    }

    [Test]
    public async Task Cancellation_between_batches_preserves_completed_writes_and_disposes_enumerator()
    {
        using var cts = new CancellationTokenSource(); var disposed = false;
        IEnumerable<RedisStreamField[]> Entries()
        {
            try { yield return Fields(0); yield return Fields(1); cts.Cancel(); yield return Fields(2); }
            finally { disposed = true; }
        }
        try { await Append(true, Entries(), 2, token: cts.Token); Assert.Fail(); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(disposed, Is.True); Assert.That(admin.XLen(key), Is.EqualTo(2));
        Assert.That(await ((IRedisClientAsync)redis).PingAsync(), Is.True);
    }

    [Test]
    public async Task Precancelled_bulk_append_does_not_enumerate_or_connect()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var enumerated = false;
        IEnumerable<RedisStreamField[]> Entries() { enumerated = true; yield return Fields(0); }
        try { await Append(true, Entries(), token: cts.Token); Assert.Fail(); }
        catch (OperationCanceledException) { }
        Assert.That(enumerated, Is.False);
        Assert.That(redis.HasConnected, Is.False);
    }

    [Test]
    public void Batch_validation_and_nested_operations_do_not_send_commands()
    {
        var client = (IRedisClient)redis;
        Assert.Throws<ArgumentOutOfRangeException>(() => client.AddStreamEntries(key, new[] { Fields(0) }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => client.AddStreamEntries(key, new[] { Fields(0) }, maxLength: -1));
        Assert.Throws<ArgumentException>(() => client.AddStreamEntries(key, new[] { Fields(0) }, approximate: true));
        Assert.Throws<ArgumentNullException>(() => client.AddStreamEntries(key, (IEnumerable<RedisStreamField[]>)null));
        using (var pipe = client.CreatePipeline())
        {
            Assert.Throws<NotSupportedException>(() => client.AddStreamEntries(key, new[] { Fields(0) }));
            Assert.Throws<NotSupportedException>(() => redis.XRange(key));
            Assert.Throws<NotSupportedException>(() => redis.XRead(new[] { new RedisStreamPosition { Stream = key } }, blockMilliseconds: 0));
        }
        using (var transaction = client.CreateTransaction())
            Assert.Throws<NotSupportedException>(() => redis.XAdd(key, Fields(0)));
        Assert.That(admin.XLen(key), Is.Zero);
        redis.Dispose();
        Assert.Throws<ObjectDisposedException>(() => client.AddStreamEntries(key, Array.Empty<RedisStreamField[]>()));
    }

    [TestCase(false)][TestCase(true)]
    public async Task Unhandled_pipeline_error_closes_unread_responses_before_client_reuse(bool async)
    {
        redis.SetValue(key, "wrong-type");
        var client = (IRedisClientAsync)redis;
        if (async)
        {
            await using var pipe = client.CreatePipeline();
            pipe.QueueCommand(r => r.XAddAsync(key, Fields(0)));
            pipe.QueueCommand(r => r.XAddAsync(key + ":good", Fields(1)));
            Assert.ThrowsAsync<RedisResponseException>(async () => await pipe.FlushAsync());
        }
        else
        {
            using var pipe = redis.CreatePipeline();
            pipe.QueueCommand(r => r.XAdd(key, Fields(0)));
            pipe.QueueCommand(r => r.XAdd(key + ":good", Fields(1)));
            Assert.Throws<RedisResponseException>(() => pipe.Flush());
        }
        Assert.That(redis.HasConnected, Is.False); Assert.That(admin.XLen(key + ":good"), Is.EqualTo(1));
        Assert.That(await client.EchoAsync("clean"), Is.EqualTo("clean"));
    }
    [TestCase(false)][TestCase(true)]
    public async Task TLS_bulk_appends_preserve_payloads_and_order(bool async)
    {
        var port = Environment.GetEnvironmentVariable("REDIS_TLS_PORT");
        var certPath = Environment.GetEnvironmentVariable("REDIS_TLS_CERT");
        if (port == null || certPath == null) Assert.Ignore("Set REDIS_TLS_PORT and REDIS_TLS_CERT for the TLS fixture");
#if NET10_0_OR_GREATER
        using var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(certPath);
#else
        using var certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);
#endif
        var previous = RedisConfig.CertificateValidationCallback;
        try
        {
            RedisConfig.CertificateValidationCallback = (_, cert, _, errors) =>
                (errors & (System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch | System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable)) == 0
                && cert.GetCertHashString() == certificate.GetCertHashString();
            redis.Dispose(); redis = new RedisClient($"rediss://localhost:{port}?ConnectTimeout=2000");
            var value = Enumerable.Range(0, 128 * 1024).Select(i => (byte)i).ToArray();
            var entry = new[] { new RedisStreamField { Name = "binary".ToUtf8Bytes(), Value = value } };
            var ids = await Append(async, Enumerable.Repeat(entry, 6), 3);
            Assert.That(ids, Is.EqualTo(admin.XRange(key).Select(x => x.Id)));
            Assert.That(admin.XRange(key).All(x => x.Fields[0].Value.SequenceEqual(value)), Is.True);
        }
        finally { RedisConfig.CertificateValidationCallback = previous; }
    }

    sealed class ReplyPeer : IAsyncDisposable
    {
        readonly TcpListener listener = new(IPAddress.Loopback, 0);
        readonly CancellationTokenSource stop = new();
        readonly Task running;
        TcpClient active;
        public readonly TaskCompletionSource<bool> Received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int XAdds, Connections;
        public bool Stall;
        public string Address => $"127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}?ConnectTimeout=2000&RetryTimeout=1000&ReceiveTimeout=2000";
        public ReplyPeer() { listener.Start(); running = Run(); }
        async Task Run()
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    active = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    Interlocked.Increment(ref Connections);
                    using (active) await Serve(active.GetStream()).ConfigureAwait(false);
                }
            }
            catch (Exception) when (stop.IsCancellationRequested) { }
        }
        async Task Serve(NetworkStream stream)
        {
            try
            {
                while (true)
                {
                    var command = await ReadCommand(stream).ConfigureAwait(false);
                    if (command == null) return;
                    if (command == "XADD")
                    {
                        if (Interlocked.Increment(ref XAdds) < 3) continue;
                        Received.TrySetResult(true);
                        if (Stall)
                        {
                            var data = new byte[256];
                            while (await stream.ReadAsync(data, 0, data.Length).ConfigureAwait(false) != 0) { }
                            Closed.TrySetResult(true);
                        }
                        else await Reply(stream, "$3\r\n1-0\r\n").ConfigureAwait(false); // Drop remaining replies.
                        return;
                    }
                    await Reply(stream, command == "INFO" ? "$21\r\nredis_version:8.0.0\r\n\r\n" : "+PONG\r\n").ConfigureAwait(false);
                }
            }
            catch (IOException) { Closed.TrySetResult(true); }
            catch (SocketException) { Closed.TrySetResult(true); }
        }
        static Task Reply(Stream stream, string text)
        {
            var data = Encoding.UTF8.GetBytes(text); return stream.WriteAsync(data, 0, data.Length);
        }
        static async Task<string> ReadCommand(Stream stream)
        {
            var line = await Line(stream).ConfigureAwait(false); if (line == null) return null;
            var count = int.Parse(line.Substring(1)); string command = null;
            for (var i = 0; i < count; i++)
            {
                line = await Line(stream).ConfigureAwait(false); if (line == null) return null;
                var data = new byte[int.Parse(line.Substring(1))]; var offset = 0;
                while (offset < data.Length)
                {
                    var read = await stream.ReadAsync(data, offset, data.Length - offset).ConfigureAwait(false);
                    if (read == 0) return null; offset += read;
                }
                if (i == 0) command = Encoding.UTF8.GetString(data);
                await Line(stream).ConfigureAwait(false);
            }
            return command;
        }
        static async Task<string> Line(Stream stream)
        {
            var data = new List<byte>(); var one = new byte[1];
            while (await stream.ReadAsync(one, 0, 1).ConfigureAwait(false) != 0)
            {
                if (one[0] == '\n') return Encoding.UTF8.GetString(data.ToArray()).TrimEnd('\r');
                data.Add(one[0]);
            }
            return null;
        }
        public async ValueTask DisposeAsync()
        {
            stop.Cancel(); active?.Dispose(); listener.Stop(); await running.ConfigureAwait(false); stop.Dispose();
        }
    }

    static async Task Prompt(Task task)
        => Assert.That(await Task.WhenAny(task, Task.Delay(3000)), Is.SameAs(task));

    [TestCase(false)][TestCase(true)]
    public async Task Lost_batch_replies_do_not_replay_generated_id_appends(bool async)
    {
        await using var peer = new ReplyPeer();
        using var client = new RedisClient(peer.Address);
        using var cts = new CancellationTokenSource(5000);
        try
        {
            if (async) await ((IRedisClientAsync)client).AddStreamEntriesAsync(key, Enumerable.Range(0, 9).Select(Fields), 3, token: cts.Token);
            else ((IRedisClient)client).AddStreamEntries(key, Enumerable.Range(0, 9).Select(Fields), 3);
            Assert.Fail("Expected dropped replies");
        }
        catch (RedisException) { }
        Assert.That(peer.Connections, Is.EqualTo(1)); Assert.That(peer.XAdds, Is.EqualTo(3));
        Assert.That(client.HasConnected, Is.False);
    }

    [Test]
    public async Task Cancellation_with_unread_batch_replies_closes_socket_and_releases_pool_lease()
    {
        await using var peer = new ReplyPeer { Stall = true };
        using var pool = new PooledRedisClientManager(new[] { peer.Address }, new[] { peer.Address },
            new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 });
        pool.RedisResolver = new BasicRedisResolver(new[] { peer.Address.ToRedisEndpoint() }, Array.Empty<RedisEndpoint>());
        var manager = (IRedisClientsManagerAsync)pool;
        await using (var client = await manager.GetClientAsync())
        {
            using var cts = new CancellationTokenSource(5000);
            var pending = client.AddStreamEntriesAsync(key, Enumerable.Range(0, 9).Select(Fields), 3, token: cts.Token).AsTask();
            await Prompt(peer.Received.Task); cts.Cancel(); await Prompt(pending);
            try { await pending; Assert.Fail(); }
            catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
            await Prompt(peer.Closed.Task);
            Assert.That(((RedisClient)client).HasConnected, Is.False);
        }
        await using var next = await manager.GetClientAsync();
        Assert.That(await next.PingAsync(), Is.True);
        Assert.That(peer.Connections, Is.EqualTo(2)); Assert.That(peer.XAdds, Is.EqualTo(3));
    }

}
