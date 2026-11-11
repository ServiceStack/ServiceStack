using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests;

/// <summary>
/// Pool waiters are woken one at a time in FIFO order, sync waiters block on their own signal rather than an
/// async continuation, and abandoned waiters never swallow a wakeup.
/// </summary>
[TestFixture, NonParallelizable]
public class RedisPoolWaiterTests
{
    static IRedisClientsManager Create(string kind) => kind switch
    {
        "pooled" => new PooledRedisClientManager(new[] { TestConfig.SingleHost }, new[] { TestConfig.SingleHost },
            new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 }) { PoolTimeout = 5000 },
        _ => new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig
            { MaxPoolSize = 1, MaxLiveConnections = 1, MaxWaitingClients = 8, PoolTimeout = 5000 }),
    };

    static async Task<string> Borrow(IRedisClientsManager manager, string name, ConcurrentQueue<string> order,
        CancellationToken token = default)
    {
        await using var client = await ((IRedisClientsManagerAsync)manager).GetClientAsync(token);
        order.Enqueue(name);
        await client.PingAsync(token);
        return name;
    }

    [TestCase("pooled")]
    [TestCase("bounded")]
    public async Task Released_clients_are_handed_to_waiters_in_arrival_order(string kind)
    {
        using var manager = Create(kind);
        var held = manager.GetClient();
        held.Ping();
        var order = new ConcurrentQueue<string>();
        var waiters = new List<Task<string>>();
        foreach (var name in new[] { "first", "second", "third", "fourth" })
        {
            waiters.Add(Borrow(manager, name, order));
            await Task.Delay(50); // establish arrival order
        }

        held.Dispose();
        var all = Task.WhenAll(waiters);
        Assert.That(await Task.WhenAny(all, Task.Delay(5000)), Is.SameAs(all), "Waiters were not all served");
        Assert.That(order, Is.EqualTo(new[] { "first", "second", "third", "fourth" }));
    }

    [TestCase("pooled", true)]
    [TestCase("pooled", false)]
    [TestCase("bounded", true)]
    [TestCase("bounded", false)]
    public async Task Cancelled_waiter_does_not_swallow_the_next_wakeup(string kind, bool cancelAfterRelease)
    {
        using var manager = Create(kind);
        var held = manager.GetClient();
        held.Ping();
        var order = new ConcurrentQueue<string>();
        using var cts = new CancellationTokenSource();
        var cancelled = Borrow(manager, "cancelled", order, cts.Token);
        await Task.Delay(50);
        var next = Borrow(manager, "next", order);
        await Task.Delay(50);

        if (cancelAfterRelease)
        {
            // Race the wakeup of the first waiter with its cancellation.
            held.Dispose();
            cts.Cancel();
        }
        else
        {
            cts.Cancel();
            held.Dispose();
        }

        try { await cancelled; }
        catch (OperationCanceledException) { }
        Assert.That(await Task.WhenAny(next, Task.Delay(5000)), Is.SameAs(next), "Wakeup was lost");
        Assert.That(await next, Is.EqualTo("next"));
    }

    [TestCase("pooled")]
    [TestCase("bounded")]
    public void Sync_waiter_on_a_dedicated_thread_is_woken_by_a_release(string kind)
    {
        using var manager = Create(kind);
        var held = manager.GetClient();
        held.Ping();
        Exception error = null;
        var acquired = new ManualResetEventSlim();
        var waiter = new Thread(() =>
        {
            try
            {
                using var client = manager.GetClient();
                client.Ping();
                acquired.Set();
            }
            catch (Exception e) { error = e; }
        }) { IsBackground = true };
        waiter.Start();
        Thread.Sleep(100);
        Assert.That(acquired.IsSet, Is.False);

        held.Dispose();
        Assert.That(acquired.Wait(3000), Is.True, error?.ToString() ?? "Sync waiter was not woken");
        waiter.Join(3000);
    }

    [TestCase("pooled")]
    [TestCase("bounded")]
    public void Sync_waiters_are_served_when_every_thread_pool_thread_is_blocked(string kind)
    {
        // Regression: sync GetClient() used to block on an async acquisition whose wakeup needed a free thread-pool
        // thread, deadlocking a saturated pool (and PoolTimeout's timer could never fire). Cap the pool, fill every
        // thread with a sync waiter, then release the only client from this (non-pool) thread.
        var waiters = Environment.ProcessorCount; // SetMaxThreads can't go below the processor count
        using var manager = kind == "pooled"
            ? new PooledRedisClientManager(new[] { TestConfig.SingleHost }, new[] { TestConfig.SingleHost },
                new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 }) { PoolTimeout = 10000 }
            : (IRedisClientsManager)new RedisManagerPool(TestConfig.SingleHost, new RedisPoolConfig
                { MaxPoolSize = 1, MaxLiveConnections = 1, MaxWaitingClients = waiters, PoolTimeout = 10000 });
        var held = manager.GetClient();
        held.Ping();

        ThreadPool.GetMinThreads(out var minWorkers, out var minIo);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIo);
        var errors = new ConcurrentQueue<Exception>();
        using var done = new CountdownEvent(waiters);
        try
        {
            Assert.That(ThreadPool.SetMinThreads(waiters, minIo), Is.True);
            Assert.That(ThreadPool.SetMaxThreads(waiters, maxIo), Is.True);
            for (var i = 0; i < waiters; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        using var client = manager.GetClient();
                        client.Ping();
                    }
                    catch (Exception e) { errors.Enqueue(e); }
                    finally { done.Signal(); }
                });
            }

            Thread.Sleep(300); // let every pool thread block in GetClient()
            held.Dispose();
            Assert.That(done.Wait(5000), Is.True,
                $"Sync waiters deadlocked: {done.CurrentCount} of {waiters} still blocked");
            Assert.That(errors, Is.Empty);
        }
        finally
        {
            // Restoring the cap also lets a regressed (deadlocked) run unwind instead of hanging the test host.
            ThreadPool.SetMaxThreads(maxWorkers, maxIo);
            ThreadPool.SetMinThreads(minWorkers, minIo);
            done.Wait(15000);
        }
    }

    [Test]
    public void ParseUri_accepts_the_legacy_idletimeout_alias_but_not_both_spellings()
    {
        Assert.That(RedisEndpoint.ParseUri("redis://localhost?idletimeout=30").IdleTimeOutSecs, Is.EqualTo(30));
        Assert.That(RedisEndpoint.ParseUri("redis://localhost?IdleTimeOutSecs=40").IdleTimeOutSecs, Is.EqualTo(40));
        Assert.Throws<ArgumentException>(() =>
            RedisEndpoint.ParseUri("redis://localhost?idletimeout=30&idletimeoutsecs=40"));
    }
}
