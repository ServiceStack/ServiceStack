#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests;

[TestFixture, NonParallelizable]
public class RedisDistributedCacheTests
{
    string host, prefix;
    RedisClient admin;
    RedisManagerPool pool;
    RedisDistributedCache cache;
    static byte[] Bytes => Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();

    [SetUp]
    public void Setup()
    {
        host = Environment.GetEnvironmentVariable("CI_REDIS") ?? "localhost:6379";
        prefix = "distributed-tests:" + Guid.NewGuid().ToString("N") + ":v1:";
        admin = new RedisClient(host);
        pool = new RedisManagerPool(host,
            new RedisPoolConfig { MaxPoolSize = 2, MaxLiveConnections = 4, MaxWaitingClients = 64 });
        cache = new RedisDistributedCache(pool, new RedisDistributedCacheOptions { KeyPrefix = prefix });
    }

    [TearDown]
    public void Cleanup()
    {
        cache.Dispose();
        pool.Dispose();
        admin.UnlinkByPattern(prefix + "*");
        admin.Dispose();
    }

    Task Set(bool async, string key, byte[] value, DistributedCacheEntryOptions options) => async
        ? cache.SetAsync(key, value, options)
        : Sync(() => cache.Set(key, value, options));

    Task<byte[]> Get(bool async, string key) => async ? cache.GetAsync(key) : Task.FromResult(cache.Get(key));
    Task Refresh(bool async, string key) => async ? cache.RefreshAsync(key) : Sync(() => cache.Refresh(key));

    static Task Sync(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    long ServerNow()
    {
        var time = admin.Time();
        return long.Parse(time[0].FromUtf8Bytes()) * 1000 + long.Parse(time[1].FromUtf8Bytes()) / 1000;
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Binary_empty_miss_remove_and_replacement_preserve_contract(bool async)
    {
        Assert.That(await Get(async, "key"), Is.Null);
        await Refresh(async, "key");
        Assert.That(admin.Exists(prefix + "key"), Is.Zero);
        foreach (var value in new[] { Bytes, Array.Empty<byte>() })
        {
            await Set(async, "key", value,
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) });
            Assert.That(await Get(async, "key"), Is.EqualTo(value));
            Assert.That(admin.HGet(prefix + "key", Utf8("data")), Is.EqualTo(value), "No JSON envelope");
            await Set(async, "key", value, new DistributedCacheEntryOptions());
            Assert.That(admin.PTtl(prefix + "key"), Is.EqualTo(-1));
            if (async) await cache.RemoveAsync("key");
            else cache.Remove("key");
            Assert.That(await Get(async, "key"), Is.Null);
        }

        Assert.That(pool.GetClientPoolActiveStates().All(x => x != 1), Is.True);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Sliding_get_and_explicit_refresh_extend_inactivity_TTL(bool async, bool refresh)
    {
        await Set(async, "key", Bytes,
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) });
        admin.PExpire(prefix + "key", 10_000);
        if (refresh) await Refresh(async, "key");
        else Assert.That(await Get(async, "key"), Is.EqualTo(Bytes));
        Assert.That(admin.PTtl(prefix + "key"), Is.InRange(50_000L, 60_000L));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Sliding_refresh_cannot_extend_absolute_deadline(bool async, bool refresh)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(1).ToOffset(TimeSpan.FromHours(5));
        await Set(async, "key", Bytes,
            new DistributedCacheEntryOptions
                { AbsoluteExpiration = deadline, SlidingExpiration = TimeSpan.FromMinutes(2) });
        var stored = long.Parse(admin.HGet(prefix + "key", Utf8("absolute")).FromUtf8Bytes());
        Assert.That(stored, Is.InRange(deadline.ToUnixTimeMilliseconds(), deadline.ToUnixTimeMilliseconds() + 1));
        admin.PExpire(prefix + "key", 10_000);
        var now = ServerNow();
        if (refresh) await Refresh(async, "key");
        else await Get(async, "key");
        Assert.That(admin.PTtl(prefix + "key"), Is.InRange(50_000L, stored - now + 1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Absolute_expiry_alone_is_not_refreshed_and_relative_option_takes_precedence(bool async)
    {
        await Set(async, "absolute", Bytes,
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(1) });
        admin.PExpire(prefix + "absolute", 10_000);
        await Refresh(async, "absolute");
        await Get(async, "absolute");
        Assert.That(admin.PTtl(prefix + "absolute"), Is.InRange(1L, 10_000L));
        await Set(async, "relative", Bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        });
        Assert.That(admin.PTtl(prefix + "relative"), Is.InRange(50_000L, 60_001L));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Real_expiry_returns_a_miss_and_refresh_does_not_recreate(bool async)
    {
        await Set(async, "key", Bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(100) });
        await Task.Delay(200);
        await Refresh(async, "key");
        Assert.That(await Get(async, "key"), Is.Null);
        Assert.That(admin.Exists(prefix + "key"), Is.Zero);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Absolute_metadata_is_enforced_even_if_the_Redis_TTL_was_removed(bool async, bool refresh)
    {
        await Set(async, "key", Bytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1), SlidingExpiration = TimeSpan.FromMinutes(2)
            });
        admin.HSet(prefix + "key", Utf8("absolute"), Utf8((ServerNow() - 1).ToString()));
        admin.Persist(prefix + "key");
        if (refresh) await Refresh(async, "key");
        else Assert.That(await Get(async, "key"), Is.Null);
        Assert.That(admin.Exists(prefix + "key"), Is.Zero);
    }

    [Test]
    public async Task Concurrent_refresh_or_get_and_replace_use_the_new_entries_own_deadline()
    {
        for (var i = 0; i < 60; i++)
        {
            var key = "race:" + i;
            await cache.SetAsync(key, Utf8("old"),
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
            var deadline = DateTimeOffset.UtcNow.AddMinutes(1);
            await Task.WhenAll(i % 2 == 0 ? cache.GetAsync(key) : cache.RefreshAsync(key),
                cache.SetAsync(key, Utf8("new"), new DistributedCacheEntryOptions { AbsoluteExpiration = deadline }));
            Assert.That(await cache.GetAsync(key), Is.EqualTo(Utf8("new")));
            Assert.That(admin.PTtl(prefix + key), Is.InRange(50_000L, 60_001L));
        }
    }

    [Test]
    public void Private_namespace_and_snapshotted_options_do_not_reinterpret_existing_cache_keys()
    {
        var logical = prefix + "logical";
        admin.SetValue(logical, "existing");
        var options = new RedisDistributedCacheOptions { KeyPrefix = prefix + "one:" };
        using var one = new RedisDistributedCache(pool, options);
        options.KeyPrefix = prefix + "two:";
        using var two = new RedisDistributedCache(pool, options);
        one.Set(logical, Bytes, new DistributedCacheEntryOptions());
        Assert.That(admin.GetValue(logical), Is.EqualTo("existing"));
        Assert.That(two.Get(logical), Is.Null);
        Assert.That(one.Get(logical), Is.EqualTo(Bytes));
        one.Remove(logical);
        Assert.That(admin.GetValue(logical), Is.EqualTo("existing"));
        admin.Del(logical);
        admin.SetValue(prefix + "foreign", "text");
        Assert.Throws<RedisResponseException>(() => cache.Get("foreign"));
        Assert.That(admin.GetValue(prefix + "foreign"), Is.EqualTo("text"));
    }

    [TestCase("Get")]
    [TestCase("Set")]
    [TestCase("Refresh")]
    [TestCase("Remove")]
    public void Precancelled_operations_do_not_acquire_a_lease(string operation)
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            if (operation == "Get") await cache.GetAsync("key", cts.Token);
            else if (operation == "Set")
                await cache.SetAsync("key", Bytes, new DistributedCacheEntryOptions(), cts.Token);
            else if (operation == "Refresh") await cache.RefreshAsync("key", cts.Token);
            else await cache.RemoveAsync("key", cts.Token);
        });
        Assert.That(pool.GetClientPoolActiveStates(), Is.EqualTo(new[] { -1, -1 }));
    }

    [Test]
    public async Task Waiting_cancellation_returns_no_late_lease_and_delayed_set_does_not_resurrect()
    {
        using var bounded = new RedisManagerPool(host,
            new RedisPoolConfig { MaxPoolSize = 1, MaxLiveConnections = 1, MaxWaitingClients = 4 });
        using var adapter = new RedisDistributedCache(bounded, new RedisDistributedCacheOptions { KeyPrefix = prefix });
        adapter.Set("key", Utf8("old"), new DistributedCacheEntryOptions());
        var held = bounded.GetClient();
        using var cts = new CancellationTokenSource();
        var cancelled = adapter.GetAsync("key", cts.Token);
        cts.Cancel();
        try
        {
            await cancelled;
            Assert.Fail("Expected cancellation");
        }
        catch (OperationCanceledException e)
        {
            Assert.That(e.CancellationToken, Is.EqualTo(cts.Token));
        }

        var delayed = adapter.SetAsync("key", Utf8("new"),
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(200) });
        await Task.Delay(300);
        held.Dispose();
        await delayed;
        Assert.That(await adapter.GetAsync("key"), Is.Null);
        Assert.That(bounded.GetStats()["clientsWaiting"], Is.EqualTo("0"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task All_operations_use_writer_leases_including_reads(bool async)
    {
        using var routed = new PooledRedisClientManager(new[] { host }, new[] { "127.0.0.1:1" },
            new RedisClientManagerConfig { MaxWritePoolSize = 1, MaxReadPoolSize = 1 });
        using var adapter = new RedisDistributedCache(routed, new RedisDistributedCacheOptions { KeyPrefix = prefix });
        if (async)
        {
            await adapter.SetAsync("key", Bytes, new DistributedCacheEntryOptions());
            Assert.That(await adapter.GetAsync("key"), Is.EqualTo(Bytes));
            await adapter.RefreshAsync("key");
            await adapter.RemoveAsync("key");
        }
        else
        {
            adapter.Set("key", Bytes, new DistributedCacheEntryOptions());
            Assert.That(adapter.Get("key"), Is.EqualTo(Bytes));
            adapter.Refresh("key");
            adapter.Remove("key");
        }

        Assert.That(routed.GetReadOnlyClientPoolActiveStates(), Is.EqualTo(new[] { -1 }));
        Assert.That(routed.GetClientPoolActiveStates(), Is.EqualTo(new[] { 0 }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Flushed_script_cache_falls_back_to_eval_once(bool async)
    {
        cache.Set("flushed", Utf8("v1"), new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) });
        admin.Custom("SCRIPT", "FLUSH"); // EVALSHA now replies NOSCRIPT

        if (async)
        {
            Assert.That(await cache.GetAsync("flushed"), Is.EqualTo(Utf8("v1")));
            await cache.SetAsync("flushed", Utf8("v2"), new DistributedCacheEntryOptions());
            await cache.RefreshAsync("flushed");
        }
        else
        {
            Assert.That(cache.Get("flushed"), Is.EqualTo(Utf8("v1")));
            cache.Set("flushed", Utf8("v2"), new DistributedCacheEntryOptions());
            cache.Refresh("flushed");
        }

        Assert.That(cache.Get("flushed"), Is.EqualTo(Utf8("v2")));
        // The EVAL fallback reloaded both scripts into the server's script cache.
        var loaded = admin.Custom("SCRIPT", "EXISTS", ScriptSha("SetScript"), ScriptSha("AccessScript"));
        Assert.That(loaded.Children.Select(x => x.Text), Is.EqualTo(new[] { "1", "1" }));
    }

    private static string ScriptSha(string name) => (string)typeof(RedisDistributedCache)
        .GetField(name + "Sha", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
        .GetValue(null);

    [TestCase(false)]
    [TestCase(true)]
    public async Task Script_denial_propagates_without_fallback_and_releases_the_lease(bool async)
    {
        var user = "dc" + Guid.NewGuid().ToString("N");
        admin.Custom("ACL", "SETUSER", user, "on", ">test-password", "~" + prefix + "*", "+@all", "-eval", "-evalsha");
        try
        {
            var endpoint = host.ToRedisEndpoint();
            endpoint.Username = user;
            endpoint.Password = "test-password";
            using var manager = new RedisManagerPool(endpoint.ToString());
            using var adapter =
                new RedisDistributedCache(manager, new RedisDistributedCacheOptions { KeyPrefix = prefix });
            cache.Set("key", Utf8("old"), new DistributedCacheEntryOptions());
            try
            {
                if (async) await adapter.SetAsync("key", Bytes, new DistributedCacheEntryOptions());
                else adapter.Set("key", Bytes, new DistributedCacheEntryOptions());
                Assert.Fail("Expected ACL error");
            }
            catch (RedisResponseException e)
            {
                Assert.That(e.Message, Does.Contain("NOPERM"));
            }

            Assert.That(cache.Get("key"), Is.EqualTo(Utf8("old")));
            Assert.That(manager.GetClientPoolActiveStates().All(x => x != 1), Is.True);
        }
        finally
        {
            admin.Custom("ACL", "DELUSER", user);
        }
    }

    [Test]
    public void Invalid_inputs_disposal_and_large_sliding_intervals_are_handled_without_overflow()
    {
        Assert.Throws<ArgumentNullException>(() => cache.Get(null));
        Assert.Throws<ArgumentNullException>(() => cache.Set("key", null, new DistributedCacheEntryOptions()));
        Assert.Throws<ArgumentNullException>(() => cache.Set("key", Bytes, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.Set("key", Bytes,
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(-1) }));
        Assert.Throws<ArgumentException>(() =>
            new RedisDistributedCache(pool, new RedisDistributedCacheOptions { KeyPrefix = "" }));
        cache.Set("large", Bytes, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.MaxValue });
        Assert.That(admin.PTtl(prefix + "large"), Is.GreaterThan(1_000_000L));
        cache.Dispose();
        cache.Dispose();
        Assert.Throws<ObjectDisposedException>(() => cache.Get("key"));
        using var lease = pool.GetClient();
        Assert.That(lease.Ping(), Is.True, "External manager is not owned by the adapter");
    }

    sealed class TrackingPool(string host) : RedisManagerPool(host)
    {
        public int DisposeCalls;

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeCalls++;
            base.Dispose(disposing);
        }
    }

    [Test]
    public void DI_uses_existing_manager_without_taking_ownership_and_supports_standard_string_extensions()
    {
        using var existing = new TrackingPool(host);
        var services = new ServiceCollection();
        services.AddSingleton<IRedisClientsManager>(existing);
        services.AddServiceStackRedisCache(x => x.KeyPrefix = prefix);
        using (var provider = services.BuildServiceProvider())
        {
            var registered = provider.GetRequiredService<IDistributedCache>();
            registered.SetString("unicode", "hello 👋");
            Assert.That(registered.GetString("unicode"), Is.EqualTo("hello 👋"));
        }

        Assert.That(existing.DisposeCalls, Is.Zero);
    }

    [Test]
    public void DI_owns_only_factory_created_managers_and_registration_is_lazy_and_idempotent()
    {
        var created = 0;
        TrackingPool owned = null;
        var services = new ServiceCollection();
        services.AddServiceStackRedisCache(_ =>
        {
            created++;
            return owned = new TrackingPool(host);
        }, x => x.KeyPrefix = prefix);
        services.AddServiceStackRedisCache(_ => throw new Exception("Second registration must not replace the first"));
        using (var provider = services.BuildServiceProvider())
        {
            Assert.That(created, Is.Zero);
            var registered = provider.GetRequiredService<IDistributedCache>();
            Assert.That(provider.GetRequiredService<IDistributedCache>(), Is.SameAs(registered));
            Assert.That(created, Is.EqualTo(1));
            registered.Set("key", Bytes);
            Assert.That(registered.Get("key"), Is.EqualTo(Bytes));
        }

        Assert.That(owned.DisposeCalls, Is.EqualTo(1));
        var memory = new ServiceCollection();
        memory.AddDistributedMemoryCache();
        memory.AddServiceStackRedisCache(_ => throw new Exception("Existing cache registration must be preserved"));
        using var other = memory.BuildServiceProvider();
        Assert.That(other.GetRequiredService<IDistributedCache>(), Is.TypeOf<MemoryDistributedCache>());
    }

    [Test]
    public void DI_owned_manager_is_disposed_once_when_registered_separately()
    {
        var owned = new TrackingPool(host);
        var services = new ServiceCollection();
        services.AddSingleton<IRedisClientsManager>(_ => owned);
        services.AddServiceStackRedisCache(x => x.KeyPrefix = prefix);
        using (var provider = services.BuildServiceProvider())
            provider.GetRequiredService<IDistributedCache>().Set("key", Bytes);
        Assert.That(owned.DisposeCalls, Is.EqualTo(1));
    }

    sealed class TestLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => new TestLogger();

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }

    sealed class TestLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;

        public void Log<TState>(LogLevel level, EventId id, TState state, Exception error,
            Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(level)) TestContext.Progress.WriteLine(formatter(state, error) + " " + error);
        }
    }

    [Test]
    public async Task HybridCache_uses_Redis_as_L2_across_independent_service_providers()
    {
        ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new TestLoggerFactory());
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddServiceStackRedisCache(host, x => x.KeyPrefix = prefix);
            services.AddHybridCache();
            return services.BuildServiceProvider();
        }

        using var first = CreateProvider();
        using var second = CreateProvider();
        var a = first.GetRequiredService<HybridCache>();
        var b = second.GetRequiredService<HybridCache>();
        Assert.That(await a.GetOrCreateAsync("hybrid", _ => new ValueTask<string>("from-redis")),
            Is.EqualTo("from-redis"));
        // Wait for publication before asking an independent L1 to consume the L2 entry.
        var published = Stopwatch.StartNew();
        while (await cache.GetAsync("hybrid") == null)
        {
            if (published.ElapsedMilliseconds > 2000) Assert.Fail("HybridCache did not publish the L2 entry");
            await Task.Delay(5);
        }

        var factories = 0;
        Assert.That(await b.GetOrCreateAsync("hybrid", _ =>
        {
            factories++;
            return new ValueTask<string>("unexpected");
        }), Is.EqualTo("from-redis"));
        Assert.That(factories, Is.Zero, "Independent L1 must load the Redis-backed L2");
        await a.RemoveAsync("hybrid");
        using var third = CreateProvider();
        Assert.That(
            await third.GetRequiredService<HybridCache>()
                .GetOrCreateAsync("hybrid", _ => new ValueTask<string>("fresh")), Is.EqualTo("fresh"));
    }
}
#endif