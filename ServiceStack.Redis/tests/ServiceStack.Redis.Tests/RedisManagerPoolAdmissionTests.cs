using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisManagerPoolAdmissionTests
{
    static RedisManagerPool Create(int retained = 1, int limit = 1, int waiters = 8, int? timeout = 3000) =>
        new(TestConfig.SingleHost, new RedisPoolConfig
        {
            MaxPoolSize = retained, MaxLiveConnections = limit, MaxWaitingClients = waiters, PoolTimeout = timeout
        });

    static Task<IRedisClientAsync> Borrow(RedisManagerPool pool, bool reader = false, CancellationToken token = default)
    {
        var manager = (IRedisClientsManagerAsync)pool;
        return (reader ? manager.GetReadOnlyClientAsync(token) : manager.GetClientAsync(token)).AsTask();
    }

    static int Stat(RedisManagerPool pool, string key) => int.Parse(pool.GetStats()[key]);

    static async Task Until(Func<bool> predicate)
    {
        var clock = Stopwatch.StartNew();
        while (!predicate())
        {
            if (clock.ElapsedMilliseconds > 3000) Assert.Fail("Timed out waiting for the expected pool state");
            await Task.Delay(5);
        }
    }

    static async Task Completes(Task task)
    {
        Assert.That(await Task.WhenAny(task, Task.Delay(4000)), Is.SameAs(task), "Acquisition did not complete");
    }

    sealed class Tracker
    {
        public int Live, Peak;
        public Action BeforeClose;
        public readonly ConcurrentBag<TrackedClient> Clients = new();

        public TrackedClient Create(RedisEndpoint endpoint)
        {
            var n = Interlocked.Increment(ref Live);
            int before;
            do
            {
                before = Peak;
                if (before >= n) break;
            } while (Interlocked.CompareExchange(ref Peak, n, before) != before);

            var client = new TrackedClient(endpoint, this);
            Clients.Add(client);
            return client;
        }
    }

    sealed class TrackedClient(RedisEndpoint endpoint, Tracker tracker) : RedisClient(endpoint)
    {
        int closed;
        public bool Closed => Volatile.Read(ref closed) != 0;

        protected override void Dispose(bool disposing)
        {
            var releasing = disposing && !IsManagedClient;
            try
            {
                if (releasing) tracker.BeforeClose?.Invoke();
                base.Dispose(disposing);
            }
            finally
            {
                if (releasing && Interlocked.Exchange(ref closed, 1) == 0)
                    Interlocked.Decrement(ref tracker.Live);
            }
        }
    }

    static Tracker Track(RedisManagerPool pool)
    {
        var tracker = new Tracker();
        ((RedisResolver)pool.RedisResolver).ClientFactory = tracker.Create;
        return tracker;
    }

    [Test]
    public void Default_pool_still_creates_unmanaged_overflow()
    {
        using var pool = new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { MaxPoolSize = 1 });
        using var first = pool.GetClient();
        using var second = pool.GetClient();
        Assert.That(((RedisClient)first).IsManagedClient, Is.True);
        Assert.That(((RedisClient)second).IsManagedClient, Is.False);
        Assert.That(second.Ping(), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void No_waiting_capacity_fails_fast_without_creating_overflow(bool async)
    {
        using var pool = Create(waiters: 0);
        var tracker = Track(pool);
        using var held = pool.GetClient();
        var clock = Stopwatch.StartNew();
        if (async) Assert.ThrowsAsync<InvalidOperationException>(async () => await Borrow(pool));
        else Assert.Throws<InvalidOperationException>(() => pool.GetClient());
        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(500));
        Assert.That(tracker.Clients.Count, Is.EqualTo(1));
        Assert.That(Stat(pool, "clientsLive"), Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Waiting_is_bounded_and_cancellation_preserves_the_callers_token(bool reader)
    {
        using var pool = Create(waiters: 1);
        await using var held = await Borrow(pool);
        using var cts = new CancellationTokenSource();
        var pending = Borrow(pool, reader, cts.Token);
        await Until(() => Stat(pool, "clientsWaiting") == 1);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Borrow(pool));
        cts.Cancel();
        await Completes(pending);
        try
        {
            await pending;
            Assert.Fail("Expected cancellation");
        }
        catch (OperationCanceledException e)
        {
            Assert.That(e.CancellationToken, Is.EqualTo(cts.Token));
        }

        Assert.That(Stat(pool, "clientsWaiting"), Is.Zero);
        await held.DisposeAsync();
        await using var next = await Borrow(pool, reader);
        Assert.That(next, Is.SameAs(held));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Precancelled_acquisition_never_creates_a_client(bool bounded)
    {
        using var pool = bounded ? Create() : new RedisManagerPool(TestConfig.SingleHost);
        var tracker = Track(pool);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await Borrow(pool, token: cts.Token));
        Assert.That(tracker.Clients.Count, Is.Zero);
        await using var next = await Borrow(pool);
        Assert.That(await next.PingAsync(), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Returning_a_client_wakes_sync_and_async_waiters(bool async)
    {
        using var pool = Create();
        var held = pool.GetClient();
        Task pending;
        if (async) pending = UseAsync();
        else
            pending = Task.Run(() =>
            {
                using var client = pool.GetClient();
                Assert.That(client.Ping(), Is.True);
            });
        await Until(() => Stat(pool, "clientsWaiting") == 1);
        held.Dispose();
        await Completes(pending);
        await pending;
        Assert.That(Stat(pool, "clientsWaiting"), Is.Zero);

        async Task UseAsync()
        {
            await using var client = await Borrow(pool);
            Assert.That(await client.PingAsync(), Is.True);
        }
    }

    [Test]
    public async Task Repeated_failover_notifications_do_not_restart_the_wait_timeout()
    {
        using var pool = Create(timeout: 150);
        using var held = pool.GetClient();
        var clock = Stopwatch.StartNew();
        var pending = Borrow(pool);
        for (var i = 0; i < 40 && !pending.IsCompleted; i++)
        {
            pool.FailoverTo(TestConfig.SingleHost);
            await Task.Delay(20);
        }

        await Completes(pending);
        Assert.ThrowsAsync<TimeoutException>(async () => await pending);
        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(700));
        Assert.That(Stat(pool, "clientsWaiting"), Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Disposal_wakes_unbounded_waiters_and_closes_retained_and_overflow_clients(bool async)
    {
        using var pool = Create(limit: 2, timeout: null);
        var tracker = Track(pool);
        using var held1 = pool.GetClient();
        using var held2 = pool.GetClient();
        var pending = async ? Borrow(pool) : Task.Run<IRedisClientAsync>(() => (IRedisClientAsync)pool.GetClient());
        await Until(() => Stat(pool, "clientsWaiting") == 1);
        pool.Dispose();
        await Completes(pending);
        Assert.ThrowsAsync<ObjectDisposedException>(async () => await pending);
        Assert.That(tracker.Live, Is.Zero);
        Assert.That(Stat(pool, "clientsLive"), Is.Zero);
        Assert.Throws<ObjectDisposedException>(() => pool.GetClient());
        Assert.ThrowsAsync<ObjectDisposedException>(async () => await Borrow(pool));
    }

    [Test]
    public async Task Retired_active_clients_keep_capacity_until_returned_after_failover()
    {
        using var pool = Create(timeout: null);
        var tracker = Track(pool);
        var held = pool.GetClient();
        var pending = Borrow(pool);
        pool.FailoverTo(TestConfig.SingleHost);
        Assert.That(pending.IsCompleted, Is.False);
        Assert.That(tracker.Clients.Count, Is.EqualTo(1));
        Assert.That(Stat(pool, "clientsLive"), Is.EqualTo(1));
        held.Dispose();
        await Completes(pending);
        await using var next = await pending;
        Assert.That(next, Is.Not.SameAs(held));
        Assert.That(tracker.Peak, Is.EqualTo(1));
        Assert.That(tracker.Clients.First(x => ReferenceEquals(x, held)).Closed, Is.True);
    }

    [TestCase("cancel")]
    [TestCase("dispose")]
    [TestCase("failover")]
    public async Task Creation_races_release_reservations_and_never_lease_stale_clients(string action)
    {
        using var pool = Create();
        var tracker = new Tracker();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cts = new CancellationTokenSource();
        var count = 0;
        ((RedisResolver)pool.RedisResolver).ClientFactory = endpoint =>
        {
            var created = tracker.Create(endpoint);
            if (Interlocked.Increment(ref count) == 1)
            {
                entered.Set();
                if (!release.Wait(3000)) throw new TimeoutException("Test creation barrier expired");
            }

            return created;
        };
        var pending = Task.Run(() => Borrow(pool, token: cts.Token));
        await Until(() => entered.IsSet);
        Assert.That(Stat(pool, "clientsCreating"), Is.EqualTo(1));
        try
        {
            if (action == "cancel") cts.Cancel();
            else if (action == "dispose") pool.Dispose();
            else pool.FailoverTo(TestConfig.SingleHost);
        }
        finally
        {
            release.Set();
        }

        await Completes(pending);
        if (action == "cancel")
        {
            try
            {
                await pending;
                Assert.Fail("Expected cancellation");
            }
            catch (OperationCanceledException e)
            {
                Assert.That(e.CancellationToken, Is.EqualTo(cts.Token));
            }
        }
        else if (action == "dispose") Assert.ThrowsAsync<ObjectDisposedException>(async () => await pending);
        else
        {
            await using var leased = await pending;
            Assert.That(tracker.Clients.Count, Is.EqualTo(2));
            Assert.That(tracker.Clients.Count(x => x.Closed), Is.EqualTo(1));
            Assert.That(tracker.Peak, Is.EqualTo(1));
        }

        Assert.That(Stat(pool, "clientsCreating"), Is.Zero);
        if (action != "failover") Assert.That(Stat(pool, "clientsLive"), Is.Zero);
        if (action == "cancel")
        {
            await using var next = await Borrow(pool);
            Assert.That(await next.PingAsync(), Is.True);
        }
    }

    [Test]
    public async Task Factory_failure_does_not_consume_capacity()
    {
        using var pool = Create();
        var resolver = (RedisResolver)pool.RedisResolver;
        var factory = resolver.ClientFactory;
        resolver.ClientFactory = _ => throw new InvalidOperationException("Test factory failure");
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Borrow(pool));
        Assert.That(Stat(pool, "clientsLive"), Is.Zero);
        Assert.That(Stat(pool, "clientsCreating"), Is.Zero);
        resolver.ClientFactory = factory;
        await using var next = await Borrow(pool);
        Assert.That(await next.PingAsync(), Is.True);
    }

    [Test]
    public async Task Returned_failed_clients_are_closed_before_replacement()
    {
        using var pool = Create();
        var tracker = Track(pool);
        using (var client = pool.GetClient())
        {
            client.SetValue("admission-wrongtype", "text");
            try
            {
                client.AddItemToList("admission-wrongtype", "item");
                Assert.Fail("Expected type error");
            }
            catch (RedisResponseException)
            {
            }
            finally
            {
                client.Remove("admission-wrongtype");
            }
        }

        await using var next = await Borrow(pool);
        Assert.That(tracker.Clients.Count, Is.EqualTo(2));
        Assert.That(tracker.Peak, Is.EqualTo(1));
    }

    [Test]
    public async Task Double_return_does_not_release_another_clients_capacity()
    {
        using var pool = Create();
        var held = pool.GetClient();
        held.Dispose();
        held.Dispose();
        await using var next = await Borrow(pool);
        Assert.That(Stat(pool, "clientsLive"), Is.EqualTo(1));
        Assert.That(pool.GetClientPoolActiveStates(), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task Saturation_caps_owned_clients_and_server_sockets_with_mixed_sync_async_borrowers()
    {
        const int limit = 4;
        using var pool = Create(retained: 2, limit: limit, waiters: 64, timeout: 10000);
        var tracker = Track(pool);
        using var observer = new RedisClient(TestConfig.SingleHost);
        var name = "admission-" + Guid.NewGuid().ToString("N");
        var tasks = Enumerable.Range(0, 48).Select(i => Task.Run(async () =>
        {
            for (var j = 0; j < 3; j++)
            {
                if (i % 2 == 0)
                {
                    await using var client = await Borrow(pool);
                    await client.SetClientAsync(name);
                    await Task.Delay(4);
                    Assert.That(await client.PingAsync(), Is.True);
                }
                else
                {
                    using var client = pool.GetClient();
                    client.SetClient(name);
                    Thread.Sleep(4);
                    Assert.That(client.Ping(), Is.True);
                }
            }
        })).ToArray();
        var all = Task.WhenAll(tasks);
        var maxSockets = 0;
        while (!all.IsCompleted)
        {
            var count = observer.Custom("CLIENT", "LIST").Text.Split('\n').Count(x => x.Contains("name=" + name + " "));
            maxSockets = Math.Max(maxSockets, count);
            Assert.That(count, Is.LessThanOrEqualTo(limit));
            Assert.That(Stat(pool, "clientsLive"), Is.LessThanOrEqualTo(limit));
            await Task.Delay(2);
        }

        await all;
        Assert.That(tracker.Peak, Is.EqualTo(limit));
        Assert.That(maxSockets, Is.GreaterThan(0));
        Assert.That(tracker.Live, Is.EqualTo(2));
        Assert.That(Stat(pool, "clientsWaiting"), Is.Zero);
        pool.Dispose();
        Assert.That(tracker.Live, Is.Zero);
    }

    [Test]
    public async Task Idle_failover_closes_old_connections_before_using_new_capacity()
    {
        using var pool = Create(retained: 2, limit: 2);
        var tracker = Track(pool);
        var first = pool.GetClient();
        var second = pool.GetClient();
        first.Dispose();
        second.Dispose();
        pool.FailoverTo(TestConfig.SingleHost);
        Assert.That(tracker.Live, Is.Zero);
        Assert.That(Stat(pool, "clientsLive"), Is.Zero);
        await using var next = await Borrow(pool);
        Assert.That(next, Is.Not.SameAs(first).And.Not.SameAs(second));
        Assert.That(await next.PingAsync(), Is.True);
    }

    [Test]
    public async Task Cancellation_racing_with_return_never_leaks_or_consumes_another_lease()
    {
        using var pool = Create();
        for (var i = 0; i < 100; i++)
        {
            var held = await Borrow(pool);
            using var cts = new CancellationTokenSource();
            var pending = Borrow(pool, token: cts.Token);
            await Task.WhenAll(Task.Run(() => held.DisposeAsync().AsTask()), Task.Run(cts.Cancel));
            try
            {
                await using var winner = await pending;
                Assert.That(await winner.PingAsync(), Is.True);
            }
            catch (OperationCanceledException e)
            {
                Assert.That(e.CancellationToken, Is.EqualTo(cts.Token));
            }

            Assert.That(Stat(pool, "clientsWaiting"), Is.Zero);
            Assert.That(pool.GetClientPoolActiveStates(), Is.EqualTo(new[] { 0 }));
        }
    }

    [Test]
    public async Task Closing_overflow_keeps_its_capacity_until_the_socket_is_closed()
    {
        using var pool = Create(limit: 2, waiters: 0);
        var tracker = Track(pool);
        using var retained = pool.GetClient();
        var overflow = pool.GetClient();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        tracker.BeforeClose = () =>
        {
            entered.Set();
            release.Wait(3000);
        };
        var closing = Task.Run(overflow.Dispose);
        await Until(() => entered.IsSet);
        try
        {
            Assert.That(Stat(pool, "clientsLive"), Is.EqualTo(2));
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Borrow(pool));
        }
        finally
        {
            release.Set();
        }

        await closing;
        tracker.BeforeClose = null;
        Assert.That(Stat(pool, "clientsLive"), Is.EqualTo(1));
        await using var replacement = await Borrow(pool);
        Assert.That(tracker.Peak, Is.EqualTo(2));
    }

    [Test]
    public async Task Default_mode_cancellation_during_creation_releases_the_reservation()
    {
        using var pool = new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { MaxPoolSize = 1 });
        using var cts = new CancellationTokenSource();
        var resolver = (RedisResolver)pool.RedisResolver;
        var factory = resolver.ClientFactory;
        resolver.ClientFactory = endpoint =>
        {
            var client = factory(endpoint);
            cts.Cancel();
            return client;
        };
        Assert.ThrowsAsync<OperationCanceledException>(async () => await Borrow(pool, token: cts.Token));
        // Cancellation before initialization disposes the unfinished client instead of retaining it.
        Assert.That(pool.GetClientPoolActiveStates(), Is.EqualTo(new[] { -1 }));
        resolver.ClientFactory = factory;
        await using var next = await Borrow(pool);
        Assert.That(await next.PingAsync(), Is.True);
    }

    [Test]
    public void Invalid_admission_options_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { MaxLiveConnections = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { MaxLiveConnections = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { MaxWaitingClients = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig { PoolTimeout = -2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RedisManagerPool(TestConfig.SingleHost,
                new RedisPoolConfig { MaxLiveConnections = 1, MaxPoolSize = 0 }));
    }
}