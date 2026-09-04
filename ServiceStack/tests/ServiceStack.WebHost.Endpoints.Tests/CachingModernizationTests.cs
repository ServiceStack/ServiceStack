using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class CachingModernizationTests
{
    [Test]
    public void MemoryCacheClient_CleaningInterval_Zero_DoesNotDivideByZero()
    {
        using var cache = new MemoryCacheClient();
        cache.CleaningInterval = 0;
        Assert.That(cache.Get<string>("nonexistent"), Is.Null);

        cache.CleaningInterval = -1;
        Assert.That(cache.Get<string>("nonexistent"), Is.Null);
    }

    [Test]
    public void MemoryCacheClient_NullGuards()
    {
        using var cache = new MemoryCacheClient();
        Assert.DoesNotThrow(() => cache.RemoveAll(null));
        Assert.DoesNotThrow(() => cache.RemoveAll(new string[0]));

        var all = cache.GetAll<string>(null);
        Assert.That(all, Is.Not.Null);
        Assert.That(all.Count, Is.EqualTo(0));

        all = cache.GetAll<string>(new string[0]);
        Assert.That(all, Is.Not.Null);
        Assert.That(all.Count, Is.EqualTo(0));

        Assert.DoesNotThrow(() => cache.SetAll<string>(null));
        Assert.DoesNotThrow(() => cache.SetAll(new Dictionary<string, string>()));
    }

    [Test]
    public async Task MemoryCacheClient_UpdateCounter_ThreadSafe()
    {
        using var cache = new MemoryCacheClient();
        const string key = "concurrency_counter";
        const int tasksCount = 20;
        const int incrementsPerTask = 100;

        var tasks = Enumerable.Range(0, tasksCount).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < incrementsPerTask; i++)
            {
                var val = cache.Increment(key, 1);
                Assert.That(val, Is.GreaterThan(0));
            }
        }));

        await Task.WhenAll(tasks);
        Assert.That(cache.Get<long>(key), Is.EqualTo(tasksCount * incrementsPerTask));

        // Test decrement
        var decVal = cache.Decrement(key, 50);
        Assert.That(decVal, Is.EqualTo((tasksCount * incrementsPerTask) - 50));
        Assert.That(cache.Get<long>(key), Is.EqualTo((tasksCount * incrementsPerTask) - 50));
    }

    [Test]
    public void MemoryCacheClient_PatternMatching_EscapesSpecialRegexChars()
    {
        using var cache = new MemoryCacheClient();
        cache.Set("user.name", "exact");
        cache.Set("user_name", "underscore");
        cache.Set("userXname", "letter");
        cache.Set("foo[bar]", "brackets");

        // Literal dot should match only dot, not any char (_)
        var dotKeys = cache.GetKeysByPattern("user.name").ToList();
        Assert.That(dotKeys, Does.Contain("user.name"));
        Assert.That(dotKeys, Does.Not.Contain("user_name"));
        Assert.That(dotKeys, Does.Not.Contain("userXname"));

        // Brackets should be literal
        var bracketKeys = cache.GetKeysByPattern("foo[bar]").ToList();
        Assert.That(bracketKeys, Does.Contain("foo[bar]"));

        // Wildcard * should match
        var wildKeys = cache.GetKeysByPattern("user.*").ToList();
        Assert.That(wildKeys, Does.Contain("user.name"));
        Assert.That(wildKeys, Does.Not.Contain("user_name"));

        // Invalid regex passed to RemoveByRegex shouldn't crash with unhandled exception
        Assert.DoesNotThrow(() => cache.RemoveByRegex("[invalid-regex("));
        Assert.DoesNotThrow(() => cache.GetKeysByRegex("[invalid-regex(").ToList());
    }

    private class DisposableCache : MemoryCacheClient, IDisposable, IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public bool IsAsyncDisposed { get; private set; }

        public new void Dispose()
        {
            IsDisposed = true;
            base.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            IsAsyncDisposed = true;
            base.Dispose();
            return default;
        }
    }

    [Test]
    public async Task CacheClientAsyncWrapper_ForwardsDisposal()
    {
        var inner = new DisposableCache();
        var wrapper = inner.AsAsync();
        if (wrapper is IDisposable disposable)
        {
            disposable.Dispose();
            Assert.That(inner.IsDisposed, Is.True);
        }

        var inner2 = new DisposableCache();
        var wrapper2 = inner2.AsAsync();
        if (wrapper2 is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            Assert.That(inner2.IsAsyncDisposed, Is.True);
        }
    }

    [Test]
    public void CacheClientWithPrefix_GetAll_StripsPrefix()
    {
        using var inner = new MemoryCacheClient();
        var prefixCache = new CacheClientWithPrefix(inner, "tenant1/");

        prefixCache.Set("k1", "v1");
        prefixCache.Set("k2", "v2");

        Assert.That(inner.Get<string>("tenant1/k1"), Is.EqualTo("v1"));
        Assert.That(inner.Get<string>("tenant1/k2"), Is.EqualTo("v2"));

        var dict = prefixCache.GetAll<string>(new[] { "k1", "k2" });
        Assert.That(dict.ContainsKey("k1"), Is.True);
        Assert.That(dict.ContainsKey("k2"), Is.True);
        Assert.That(dict.ContainsKey("tenant1/k1"), Is.False);
        Assert.That(dict["k1"], Is.EqualTo("v1"));
        Assert.That(dict["k2"], Is.EqualTo("v2"));

        // Null guards
        Assert.That(prefixCache.GetAll<string>(null).Count, Is.EqualTo(0));
        Assert.DoesNotThrow(() => prefixCache.SetAll<string>(null));
        Assert.DoesNotThrow(() => prefixCache.RemoveAll(null));
    }

    [Test]
    public async Task CacheClientWithPrefixAsync_GetAllAsync_StripsPrefix()
    {
        using var inner = new MemoryCacheClient();
        var prefixCache = new CacheClientWithPrefixAsync(inner.AsAsync(), "tenant1/");

        await prefixCache.SetAsync("k1", "v1");
        await prefixCache.SetAsync("k2", "v2");

        Assert.That(inner.Get<string>("tenant1/k1"), Is.EqualTo("v1"));
        Assert.That(inner.Get<string>("tenant1/k2"), Is.EqualTo("v2"));

        var dict = await prefixCache.GetAllAsync<string>(new[] { "k1", "k2" });
        Assert.That(dict.ContainsKey("k1"), Is.True);
        Assert.That(dict.ContainsKey("k2"), Is.True);
        Assert.That(dict.ContainsKey("tenant1/k1"), Is.False);
        Assert.That(dict["k1"], Is.EqualTo("v1"));
        Assert.That(dict["k2"], Is.EqualTo("v2"));

        // Null guards
        var empty = await prefixCache.GetAllAsync<string>(null);
        Assert.That(empty.Count, Is.EqualTo(0));
        Assert.DoesNotThrowAsync(async () => await prefixCache.SetAllAsync<string>(null));
        Assert.DoesNotThrowAsync(async () => await prefixCache.RemoveAllAsync(null));
    }

    [Test]
    public async Task MultiCacheClient_SetAsync_OverwritesCorrectly()
    {
        Assert.Throws<ArgumentNullException>(() => new MultiCacheClient(null));
        Assert.Throws<ArgumentNullException>(() => new MultiCacheClient(new ICacheClient[0]));

        using var c1 = new MemoryCacheClient();
        using var c2 = new MemoryCacheClient();
        using var multi = new MultiCacheClient(c1, c2);

        // Pre-populate c1 with initial value
        c1.Set("k1", "initial");
        Assert.That(c1.Get<string>("k1"), Is.EqualTo("initial"));

        // SetAsync should overwrite "initial" with "updated" on both clients
        await multi.SetAsync("k1", "updated", TimeSpan.FromMinutes(5));
        Assert.That(c1.Get<string>("k1"), Is.EqualTo("updated"));
        Assert.That(c2.Get<string>("k1"), Is.EqualTo("updated"));

        // Null checks
        Assert.That(multi.GetAll<string>(null).Count, Is.EqualTo(0));
        Assert.That((await multi.GetAllAsync<string>(null)).Count, Is.EqualTo(0));
        Assert.DoesNotThrow(() => multi.SetAll<string>(null));
        Assert.DoesNotThrowAsync(async () => await multi.SetAllAsync<string>(null));
        Assert.DoesNotThrow(() => multi.RemoveAll(null));
        Assert.DoesNotThrowAsync(async () => await multi.RemoveAllAsync(null));
    }

    [Test]
    public async Task CacheClientExtensions_NullGuardsAndFallback()
    {
        using var cache = new MemoryCacheClient();
        var asyncCache = cache.AsAsync();

        // Null/empty ClearCaches
        Assert.DoesNotThrow(() => cache.ClearCaches((string[])null));
        Assert.DoesNotThrow(() => cache.ClearCaches(new string[0]));
        Assert.DoesNotThrowAsync(async () => await asyncCache.ClearCachesAsync(null));
        Assert.DoesNotThrowAsync(async () => await asyncCache.ClearCachesAsync(new string[0]));

        // HasValidCache when HttpCacheFeature is null (or not configured)
        var isValid = cache.HasValidCache(null, "key", DateTime.UtcNow, out var lastMod);
        Assert.That(isValid, Is.False);
        Assert.That(lastMod, Is.Null);

        var validCache = await asyncCache.HasValidCacheAsync(null, "key", DateTime.UtcNow);
        Assert.That(validCache.IsValid, Is.False);
    }
}
