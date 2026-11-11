using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisReliabilityTests
{
    bool verify;
    int version;
    int? assumption;
    [SetUp] public void Before()
    {
        verify = RedisConfig.VerifyMasterConnections;
        version = RedisNativeClient.ServerVersionNumber;
        assumption = RedisConfig.AssumeServerVersion;
        RedisConfig.VerifyMasterConnections = false;
        RedisConfig.AssumeServerVersion = RedisNativeClient.ServerVersionNumber = 8100;
    }
    [TearDown] public void After()
    {
        RedisConfig.VerifyMasterConnections = verify;
        RedisNativeClient.ServerVersionNumber = version;
        RedisConfig.AssumeServerVersion = assumption;
    }

    static IRedisClientsManager Manager(string kind, string host = "old-host:6379") => kind switch
    {
        "pooled" => new PooledRedisClientManager(new[] { host }, new[] { host },
            new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 }) { PoolTimeout = 3000 },
        "bounded" => new RedisManagerPool(host, new RedisPoolConfig { MaxPoolSize = 1, MaxLiveConnections = 2, MaxWaitingClients = 3 }),
        "basic" => new BasicRedisClientManager(host),
        _ => new RedisManagerPool(host, new RedisPoolConfig { MaxPoolSize = 1 })
    };
    static void Resolver(IRedisClientsManager manager, IRedisResolver resolver)
    {
        if (manager is PooledRedisClientManager p) p.RedisResolver = resolver;
        else if (manager is BasicRedisClientManager b) b.RedisResolver = resolver;
        else ((RedisManagerPool)manager).RedisResolver = resolver;
    }
    static Task<IRedisClientAsync> Borrow(IRedisClientsManager manager, bool reader = false, bool sync = false)
        => sync ? Task.Run(() => (IRedisClientAsync)(reader ? manager.GetReadOnlyClient() : manager.GetClient()))
            : (reader ? ((IRedisClientsManagerAsync)manager).GetReadOnlyClientAsync()
                : ((IRedisClientsManagerAsync)manager).GetClientAsync()).AsTask();
    static async Task Prompt(Task task)
        => Assert.That(await Task.WhenAny(task, Task.Delay(1500)), Is.SameAs(task), "Operation did not finish promptly.");
    static async Task Until(Func<bool> predicate)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate() && timer.ElapsedMilliseconds < 1500) await Task.Delay(10);
        Assert.That(predicate(), Is.True);
    }

    sealed class TrackedClient(RedisEndpoint endpoint) : RedisClient(endpoint)
    {
        int disposed;
        public bool WasDisposed => Volatile.Read(ref disposed) != 0;
        protected override void Dispose(bool disposing)
        {
            var detached = !IsManagedClient;
            try { base.Dispose(disposing); }
            finally { if (detached) Interlocked.Exchange(ref disposed, 1); }
        }
    }

    sealed class GatedResolver : RedisResolver
    {
        public readonly TaskCompletionSource<bool>[] Entered = { new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously) };
        public readonly TaskCompletionSource<bool>[] Release = { new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously) };
        public TrackedClient First;
        public bool FailFirst;
        int calls;
        public GatedResolver() : base(new[] { "old-host:6379" }, new[] { "old-host:6379" }) { }
        public override RedisClient CreateRedisClient(RedisEndpoint endpoint, bool master)
            => CreateRedisClientAsync(endpoint, master).GetAwaiter().GetResult();
        public override async ValueTask<RedisClient> CreateRedisClientAsync(RedisEndpoint endpoint, bool master,
            CancellationToken token = default, Action<RedisClient> configure = null)
        {
            var n = Interlocked.Increment(ref calls) - 1;
            var client = new TrackedClient(endpoint);
            configure?.Invoke(client);
            if (n == 0) First = client;
            if (n < 2) { Entered[n].TrySetResult(true); await Release[n].Task.ConfigureAwait(false); }
            if (n == 0 && FailFirst) { client.Dispose(); throw new InvalidOperationException("creation failed"); }
            return client;
        }
    }

    [TestCase("pooled", false, false, false)][TestCase("pooled", true, false, false)]
    [TestCase("manager", false, false, false)][TestCase("bounded", false, false, false)]
    [TestCase("pooled", false, true, false)][TestCase("pooled", true, true, false)]
    [TestCase("manager", false, true, false)][TestCase("bounded", false, true, false)]
    [TestCase("pooled", false, false, true)][TestCase("pooled", true, false, true)]
    [TestCase("manager", false, false, true)][TestCase("bounded", false, false, true)]
    [TestCase("pooled", false, true, true)][TestCase("pooled", true, true, true)]
    [TestCase("manager", false, true, true)][TestCase("bounded", false, true, true)]
    public async Task Failover_rejects_stale_creation_and_preserves_the_new_reservation(string kind, bool reader, bool sync, bool fail)
    {
        using var manager = Manager(kind);
        var resolver = new GatedResolver { FailFirst = fail }; Resolver(manager, resolver);
        var old = Borrow(manager, reader, sync); await Prompt(resolver.Entered[0].Task);
        ((IRedisFailover)manager).FailoverTo("new-host:6379");
        var replacement = Borrow(manager, reader, sync); await Prompt(resolver.Entered[1].Task);
        try
        {
            resolver.Release[0].TrySetResult(true);
            await Until(() => resolver.First.WasDisposed);
            if (fail)
            {
                await Prompt(old);
                try { await old; Assert.Fail("Expected failed creation."); } catch (InvalidOperationException) { }
                var competing = Borrow(manager, reader, sync);
                if (kind == "pooled") Assert.That(competing.IsCompleted, Is.False, "Old cleanup released another reservation.");
                else
                {
                    await Prompt(competing);
                    await using var overflow = await competing;
                    if (kind == "manager") Assert.That(((RedisClient)overflow).IsManagedClient, Is.False);
                }
                resolver.Release[1].TrySetResult(true);
                await using var current = await replacement;
                Assert.That(((RedisClient)current).Host, Is.EqualTo("new-host"));
                if (kind == "pooled")
                {
                    await current.DisposeAsync(); await Prompt(competing);
                    await using var next = await competing; Assert.That(next, Is.SameAs(current));
                }
            }
            else
            {
                resolver.Release[1].TrySetResult(true);
                await Prompt(replacement); await using var current = await replacement;
                Assert.That(((RedisClient)current).Host, Is.EqualTo("new-host"));
                await current.DisposeAsync(); await Prompt(old);
                await using var retried = await old;
                Assert.That(((RedisClient)retried).Host, Is.EqualTo("new-host"));
            }
        }
        finally { resolver.Release[0].TrySetResult(true); resolver.Release[1].TrySetResult(true); }
    }

    class LegacyFactoryResolver : RedisResolver
    {
        public int Calls;
        public LegacyFactoryResolver() : base(new[] { "old-host:6379" }, new[] { "old-host:6379" }) { }
        public override RedisClient CreateRedisClient(RedisEndpoint endpoint, bool master)
        { Calls++; return new RedisClient(new RedisEndpoint(endpoint.Host, endpoint.Port) { Ssl = true, Username = "policy-user", Password = "policy-password", NamespacePrefix = "policy:" }); }
    }
    sealed class LegacyRouteResolver : RedisResolver
    {
        public LegacyRouteResolver() : base(new[] { "old-host:6379" }, new[] { "old-host:6379" }) { }
        public override RedisClient CreateRedisClient(RedisEndpoint endpoint, bool master)
            => new(new RedisEndpoint(master ? "policy-master" : "policy-replica", 6379) { Ssl = true, Username = "policy-user", Password = "policy-password", NamespacePrefix = "policy:" });
    }
    sealed class ExplicitAsyncResolver : LegacyFactoryResolver
    {
        public override ValueTask<RedisClient> CreateRedisClientAsync(RedisEndpoint endpoint, bool master,
            CancellationToken token = default, Action<RedisClient> configure = null)
        {
            var client = new RedisClient(endpoint) { NamespacePrefix = "async-policy:" };
            configure?.Invoke(client); return new ValueTask<RedisClient>(client);
        }
    }

    [TestCase("pooled", false)][TestCase("pooled", true)][TestCase("basic", false)][TestCase("basic", true)]
    [TestCase("manager", false)][TestCase("bounded", false)]
    public async Task Async_managers_preserve_legacy_factory_and_route_overrides(string kind, bool reader)
    {
        foreach (IRedisResolver resolver in new IRedisResolver[] { new LegacyFactoryResolver(), new LegacyRouteResolver() })
        {
            using var manager = Manager(kind); Resolver(manager, resolver);
            await using var borrowed = await Borrow(manager, reader);
            var client = (RedisClient)borrowed;
            Assert.That(client.Ssl, Is.True); Assert.That(client.Username, Is.EqualTo("policy-user"));
            Assert.That(client.Password, Is.EqualTo("policy-password")); Assert.That(client.NamespacePrefix, Is.EqualTo("policy:"));
            if (resolver is LegacyRouteResolver) Assert.That(client.Host, Is.EqualTo(reader ? "policy-replica" : "policy-master"));
        }
    }
    [Test] public async Task Explicit_async_override_takes_precedence_over_inherited_sync_policy()
    {
        using var manager = Manager("pooled"); var resolver = new ExplicitAsyncResolver(); Resolver(manager, resolver);
        await using var borrowed = await Borrow(manager);
        Assert.That(((RedisClient)borrowed).NamespacePrefix, Is.EqualTo("async-policy:")); Assert.That(resolver.Calls, Is.Zero);
    }

    sealed class OverloadedLegacyResolver : LegacyFactoryResolver
    {
        public RedisClient CreateRedisClient(RedisEndpoint endpoint) => throw new InvalidOperationException("Wrong overload.");
    }
    sealed class TrackingLegacyResolver : RedisResolver
    {
        public TrackedClient Created;
        public TrackingLegacyResolver() : base(new[] { "old-host:6379" }, null) { }
        public override RedisClient CreateRedisClient(RedisEndpoint endpoint, bool master) => Created = new TrackedClient(endpoint);
    }
    [Test] public async Task Legacy_override_detection_tolerates_unrelated_overloads()
    {
        var resolver = new OverloadedLegacyResolver();
        using var client = await resolver.CreateMasterClientAsync(0);
        Assert.That(client.Username, Is.EqualTo("policy-user")); Assert.That(resolver.Calls, Is.EqualTo(1));
    }
    [Test] public async Task Precancelled_legacy_resolution_does_not_invoke_the_factory()
    {
        var resolver = new LegacyFactoryResolver(); using var cts = new CancellationTokenSource(); cts.Cancel();
        try { await resolver.CreateMasterClientAsync(0, cts.Token); Assert.Fail("Expected cancellation."); }
        catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(resolver.Calls, Is.Zero);
    }
    [TestCase(false)][TestCase(true)]
    public async Task Legacy_configuration_failure_or_cancellation_disposes_the_created_client(bool cancel)
    {
        var resolver = new TrackingLegacyResolver(); using var cts = new CancellationTokenSource();
        try
        {
            await resolver.CreateMasterClientAsync(0, cts.Token, _ =>
            {
                if (cancel) cts.Cancel(); else throw new InvalidOperationException("configuration failed");
            });
            Assert.Fail("Expected configuration failure.");
        }
        catch (OperationCanceledException ex) when (cancel) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        catch (InvalidOperationException) when (!cancel) { }
        Assert.That(resolver.Created.WasDisposed, Is.True);
    }

    [TestCase("AUTH")][TestCase("SELECT")][TestCase("CLIENT")]
    public async Task Setup_EOF_does_not_open_a_synchronous_replacement_connection(string command)
    {
        await using var server = new RedisAsyncConnectionTests.Server((n, args) => Task.FromResult(args[0] == command ? null : "+OK\r\n"));
        using var client = new RedisClient("127.0.0.1", server.Port, "password") { Db = 1, Client = "eof-test", ConnectTimeout = 300, ReceiveTimeout = 2500 };
        var pending = client.ConnectAsync().AsTask(); await Prompt(pending);
        try { await pending; Assert.Fail("Expected setup EOF."); } catch (RedisException) { }
        Assert.That(server.Connections, Is.EqualTo(1)); Assert.That(client.HasConnected, Is.False);
    }
    [Test] public async Task EOF_retry_uses_async_authentication_and_preserves_caller_cancellation()
    {
        RedisAsyncConnectionTests.Server server = null;
        await using var fixture = server = new RedisAsyncConnectionTests.Server((n, args) =>
        {
            if (n == 1 && args[0] == "PING") { server.PauseCommand = "AUTH"; return Task.FromResult<string>(null); }
            return Task.FromResult(args[0] == "AUTH" ? "+OK\r\n" : "+PONG\r\n");
        });
        using var client = new RedisClient("127.0.0.1", server.Port, "password") { RetryTimeout = 5000, ConnectTimeout = 5000, ReceiveTimeout = 2500 };
        using var cts = new CancellationTokenSource(5000);
        var pending = ((IRedisClientAsync)client).PingAsync(cts.Token).AsTask(); await Prompt(server.Paused.Task); cts.Cancel();
        await Prompt(pending);
        try { await pending; Assert.Fail("Expected cancellation."); } catch (OperationCanceledException ex) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(server.Connections, Is.EqualTo(2)); Assert.That(client.HasConnected, Is.False);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Pipeline_and_bulk_receive_timeouts_close_the_connection(bool bulk)
    {
        await using var server = new RedisAsyncConnectionTests.Server((_, args) => Task.FromResult(args[0] == "PING" ? "+PONG\r\n" : "+OK\r\n")) { PauseCommand = bulk ? "XADD" : "PING" };
        using var client = new RedisClient(server.Address) { ReceiveTimeout = 150, SendTimeout = 5000 };
        await client.ConnectAsync();
        Task pending;
        if (bulk) pending = ((IRedisClientAsync)client).AddStreamEntriesAsync("events", new[] { Fields() }).AsTask();
        else
        {
            var pipe = ((IRedisClientAsync)client).CreatePipeline();
            pipe.QueueCommand(r => r.PingAsync(), _ => { }, _ => { }); // Even a swallowing callback cannot hide timeout.
            pending = FlushAndDispose(pipe);
        }
        await Prompt(pending);
        try { await pending; Assert.Fail("Expected receive timeout."); } catch (TimeoutException e) { Assert.That(e.Message, Does.Contain("ReceiveTimeout")); }
        Assert.That(client.HasConnected, Is.False); server.PauseCommand = null;
        Assert.That(await ((IRedisClientAsync)client).PingAsync(), Is.True);
    }
    static async Task FlushAndDispose(IRedisPipelineAsync pipe)
    { await using (pipe) await pipe.FlushAsync(); }
    static RedisStreamField[] Fields(int size = 1) => new[] { new RedisStreamField { Name = new byte[] { 102 }, Value = new byte[size] } };

    [TestCase(false)][TestCase(true)]
    public async Task Bulk_send_timeout_and_caller_cancellation_interrupt_a_blocked_socket(bool cancel)
    {
        await using var server = new RedisAsyncConnectionTests.Server((_, args) => Task.FromResult("+ready\r\n")) { PauseAfterReply = "ECHO" };
        using var client = new RedisClient(server.Address) { SendTimeout = cancel ? 5000 : 150, ReceiveTimeout = 5000 };
        var asyncClient = (IRedisClientAsync)client;
        await asyncClient.EchoAsync("ready"); await Prompt(server.Paused.Task);
        ((Socket)typeof(RedisNativeClient).GetField("socket", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client)).SendBufferSize = 1024;
        using var cts = new CancellationTokenSource(5000);
        var pending = asyncClient.AddStreamEntriesAsync("events", new[] { Fields(8 * 1024 * 1024) }, token: cts.Token).AsTask();
        if (cancel) { await Task.Delay(50); cts.Cancel(); }
        await Prompt(pending);
        try { await pending; Assert.Fail("Expected interrupted send."); }
        catch (OperationCanceledException ex) when (cancel) { Assert.That(ex.CancellationToken, Is.EqualTo(cts.Token)); }
        catch (TimeoutException ex) when (!cancel) { Assert.That(ex.Message, Does.Contain("SendTimeout")); }
        Assert.That(client.HasConnected, Is.False); Assert.That(server.Commands.Count(x => x[0] == "XADD"), Is.Zero);
    }

    [TestCase(false)][TestCase(true)]
    public async Task Failover_during_reused_database_reset_retries_against_the_new_topology(bool reader)
    {
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pause = false;
        await using var server = new RedisAsyncConnectionTests.Server(async (_, args) =>
        {
            if (pause && args[0] == "SELECT") { entered.TrySetResult(true); await release.Task.ConfigureAwait(false); }
            return "+OK\r\n";
        });
        var oldHost = $"127.0.0.1:{server.Port}";
        using var manager = new PooledRedisClientManager(new[] { oldHost }, new[] { oldHost },
            new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1, DefaultDb = 0 });
        var first = await Borrow(manager, reader);
        await first.SelectAsync(1); await first.DisposeAsync(); pause = true;
        var resetting = Borrow(manager, reader); await Prompt(entered.Task);
        manager.FailoverTo($"localhost:{server.Port}"); release.TrySetResult(true);
        await Prompt(resetting); await using var next = await resetting;
        Assert.That(next, Is.Not.SameAs(first)); Assert.That(((RedisClient)next).Host, Is.EqualTo("localhost"));
    }

    [TestCase(":123")][TestCase(":123\rX")][TestCase(":123\n")]
    public async Task Partial_or_malformed_pipeline_replies_close_without_reconnection_or_replay(string reply)
    {
        await using var server = new RedisAsyncConnectionTests.Server((_, args) => Task.FromResult(args[0] == "XLEN" ? reply : "+PONG\r\n")) { CloseAfterCommand = "XLEN" };
        using var client = new RedisClient(server.Address) { ReceiveTimeout = 500, RetryTimeout = 5000 };
        await client.ConnectAsync();
        await using (var pipe = ((IRedisClientAsync)client).CreatePipeline())
        {
            pipe.QueueCommand(r => r.XLenAsync("events"), _ => { });
            try { await pipe.FlushAsync(); Assert.Fail("Expected invalid protocol."); }
            catch (Exception ex) when (ex is RedisException || ex is IOException) { }
        }
        Assert.That(client.HasConnected, Is.False); Assert.That(server.Connections, Is.EqualTo(1));
        Assert.That(server.Commands.Count(x => x[0] == "XLEN"), Is.EqualTo(1));
        Assert.That(await ((IRedisClientAsync)client).PingAsync(), Is.True);
    }

    [TestCase(false, false, false)][TestCase(false, false, true)][TestCase(false, true, false)][TestCase(false, true, true)]
    [TestCase(true, false, false)][TestCase(true, false, true)][TestCase(true, true, false)][TestCase(true, true, true)]
    public async Task Queued_key_commands_preserve_empty_and_missing_values(bool async, bool transaction, bool native)
    {
        using var client = new RedisClient(TestConfig.SingleHost);
        if (Version.Parse(client.Info["redis_version"]) < new Version(6, 2))
            Assert.Ignore("GETEX/GETDEL require Redis 6.2+");
        var key = "reliability:" + Guid.NewGuid().ToString("N");
        client.Set(key, Array.Empty<byte>()); byte[] empty = null, missing = new byte[] { 1 }; string emptyText = null, missingText = "marker";
        try
        {
            if (async)
            {
                var redis = (IRedisClientAsync)client;
                await client.ConnectAsync();
                IRedisQueueableOperationAsync queue;
                IRedisPipelineAsync pipe = null; IRedisTransactionAsync tx = null;
                if (transaction) queue = tx = await redis.CreateTransactionAsync(); else queue = pipe = redis.CreatePipeline();
                await using var dispose = (IAsyncDisposable)queue;
                if (native)
                {
                    queue.QueueCommand(r => ((IRedisNativeKeyCommandsAsync)r).GetExAsync(key), (byte[] b) => empty = b);
                    queue.QueueCommand(r => ((IRedisNativeKeyCommandsAsync)r).GetDelAsync(key), (byte[] b) => empty = b);
                    queue.QueueCommand(r => ((IRedisNativeKeyCommandsAsync)r).GetDelAsync(key), (byte[] b) => missing = b);
                }
                else
                {
                    queue.QueueCommand(r => r.GetAndExpireValueAsync(key), s => emptyText = s);
                    queue.QueueCommand(r => r.GetAndDeleteValueAsync(key), s => emptyText = s);
                    queue.QueueCommand(r => r.GetAndDeleteValueAsync(key), s => missingText = s);
                }
                if (transaction) Assert.That(await tx.CommitAsync(), Is.True); else await pipe.FlushAsync();
            }
            else
            {
                IRedisQueueableOperation queue;
                IRedisPipeline pipe = null; IRedisTransaction tx = null;
                if (transaction) queue = tx = client.CreateTransaction(); else queue = pipe = client.CreatePipeline();
                using var dispose = (IDisposable)queue;
                if (native)
                {
                    queue.QueueCommand(r => ((IRedisNativeKeyCommands)r).GetEx(key), (byte[] b) => empty = b);
                    queue.QueueCommand(r => ((IRedisNativeKeyCommands)r).GetDel(key), (byte[] b) => empty = b);
                    queue.QueueCommand(r => ((IRedisNativeKeyCommands)r).GetDel(key), (byte[] b) => missing = b);
                }
                else
                {
                    queue.QueueCommand(r => r.GetAndExpireValue(key), s => emptyText = s);
                    queue.QueueCommand(r => r.GetAndDeleteValue(key), s => emptyText = s);
                    queue.QueueCommand(r => r.GetAndDeleteValue(key), s => missingText = s);
                }
                if (transaction) Assert.That(tx.Commit(), Is.True); else pipe.Flush();
            }
            if (native) { Assert.That(empty, Is.Not.Null.And.Empty); Assert.That(missing, Is.Null); }
            else { Assert.That(emptyText, Is.EqualTo("")); Assert.That(missingText, Is.Null); }
        }
        finally { client.Del(key); }
    }
}
