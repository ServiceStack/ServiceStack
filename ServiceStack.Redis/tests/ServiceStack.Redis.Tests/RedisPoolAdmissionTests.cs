using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisPoolAdmissionTests
{
    sealed class DisposalTrackingClient(RedisEndpoint endpoint) : RedisClient(endpoint)
    {
        public bool DisposedUnmanaged { get; private set; }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsManagedClient) DisposedUnmanaged = true;
            base.Dispose(disposing);
        }
    }

    static PooledRedisClientManager CreateManager() => new(
        [TestConfig.SingleHost], [TestConfig.SingleHost],
        new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 }) { PoolTimeout = 3000 };

    static Task<IRedisClientAsync> Borrow(PooledRedisClientManager manager, bool reader, CancellationToken token = default)
    {
        var asyncManager = (IRedisClientsManagerAsync)manager;
        return (reader ? asyncManager.GetReadOnlyClientAsync(token) : asyncManager.GetClientAsync(token)).AsTask();
    }

    static async Task CompletesPromptly(Task task)
    {
        Assert.That(await Task.WhenAny(task, Task.Delay(1500)), Is.SameAs(task), "Pool wait did not complete promptly");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cancellation_stops_waiting_without_consuming_a_lease(bool reader)
    {
        using var manager = CreateManager();
        await using var held = await Borrow(manager, reader);
        using var cts = new CancellationTokenSource();
        var pending = Borrow(manager, reader, cts.Token);
        Assert.That(pending.IsCompleted, Is.False);
        cts.Cancel();
        await CompletesPromptly(pending);
        try { await pending; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException e) { Assert.That(e.CancellationToken, Is.EqualTo(cts.Token)); }
        await held.DisposeAsync();
        await using var next = await Borrow(manager, reader);
        Assert.That(next, Is.SameAs(held));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Precancelled_acquisition_does_not_reserve_a_slot(bool reader)
    {
        using var manager = CreateManager();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await Borrow(manager, reader, cts.Token); Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException) { }
        Assert.That(reader ? manager.GetReadOnlyClientPoolActiveStates() : manager.GetClientPoolActiveStates(), Is.EqualTo(new[] { -1 }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cancellation_during_client_creation_releases_the_reserved_slot(bool reader)
    {
        using var manager = CreateManager();
        using var cts = new CancellationTokenSource();
        var resolver = (RedisResolver)manager.RedisResolver;
        var factory = resolver.ClientFactory;
        DisposalTrackingClient created = null;
        resolver.ClientFactory = endpoint =>
        {
            created = new DisposalTrackingClient(endpoint);
            cts.Cancel();
            return created;
        };
        try { await Borrow(manager, reader, cts.Token); Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException e) { Assert.That(e.CancellationToken, Is.EqualTo(cts.Token)); }
        Assert.That(created, Is.Not.Null);
        Assert.That(created.DisposedUnmanaged, Is.True);
        Assert.That(reader ? manager.GetReadOnlyClientPoolActiveStates() : manager.GetClientPoolActiveStates(), Is.EqualTo(new[] { -1 }));
        resolver.ClientFactory = factory;
        await using var next = await Borrow(manager, reader);
        Assert.That(next, Is.Not.SameAs(created));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Returning_a_client_wakes_a_waiter(bool reader)
    {
        using var manager = CreateManager();
        await using var held = await Borrow(manager, reader);
        var pending = Borrow(manager, reader);
        await held.DisposeAsync();
        await CompletesPromptly(pending);
        await using var next = await pending;
        Assert.That(next, Is.SameAs(held));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Repeated_wakeups_do_not_restart_the_pool_timeout(bool reader)
    {
        using var manager = CreateManager();
        manager.PoolTimeout = 200;
        await using var held = await Borrow(manager, reader);
        var clock = Stopwatch.StartNew();
        var pending = Borrow(manager, reader);
        using var unrelated = new RedisClient(TestConfig.SingleHost);
        for (var i = 0; i < 20 && !pending.IsCompleted; i++)
        {
            await Task.Delay(50);
            manager.DisposeClient(unrelated); // A broadcast without an available slot.
        }
        await CompletesPromptly(pending);
        try { await pending; Assert.Fail("Expected pool timeout"); }
        catch (TimeoutException) { }
        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(900));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Disposal_ends_an_unbounded_wait(bool reader)
    {
        using var manager = CreateManager();
        manager.PoolTimeout = null;
        await using var held = await Borrow(manager, reader);
        var pending = Borrow(manager, reader);
        manager.Dispose();
        await CompletesPromptly(pending);
        try { await pending; Assert.Fail("Expected disposed manager"); }
        catch (ObjectDisposedException) { }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Failover_wakes_a_waiter_to_use_the_replacement_pool(bool reader)
    {
        using var manager = CreateManager();
        manager.PoolTimeout = null;
        await using var held = await Borrow(manager, reader);
        var pending = Borrow(manager, reader);
        manager.FailoverTo(TestConfig.SingleHost);
        await CompletesPromptly(pending);
        await using var next = await pending;
        Assert.That(next, Is.Not.SameAs(held));
    }
}
