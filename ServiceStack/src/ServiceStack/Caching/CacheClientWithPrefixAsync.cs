using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Caching;

/// <summary>
/// Decorates the ICacheClient (and its siblings) prefixing every key with the given prefix
/// 
/// Useful for multi-tenant environments
/// </summary>
public class CacheClientWithPrefixAsync : ICacheClientAsync, IRemoveByPatternAsync, IDisposable
{
    private readonly string prefix;
    private readonly ICacheClientAsync cache;

    public CacheClientWithPrefixAsync(ICacheClientAsync cache, string prefix)
    {
        this.prefix = prefix;
        this.cache = cache;
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken token=default)
    {
        return await cache.RemoveAsync(EnsurePrefix(key), token).ConfigAwait();
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken token=default)
    {
        return await cache.GetAsync<T>(EnsurePrefix(key), token).ConfigAwait();
    }

    public async Task<long> IncrementAsync(string key, uint amount, CancellationToken token=default)
    {
        return await cache.IncrementAsync(EnsurePrefix(key), amount, token).ConfigAwait();
    }

    public async Task<long> DecrementAsync(string key, uint amount, CancellationToken token=default)
    {
        return await cache.DecrementAsync(EnsurePrefix(key), amount, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await cache.AddAsync(EnsurePrefix(key), value, token).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await cache.SetAsync(EnsurePrefix(key), value, token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await cache.ReplaceAsync(EnsurePrefix(key), value, token).ConfigAwait();
    }

    public async Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token=default)
    {
        if (values == null) return;
        await cache.SetAllAsync(values.ToDictionary(
            x => EnsurePrefix(x.Key), x => x.Value), token).ConfigAwait();
    }

    public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token=default)
    {
        if (keys == null) return new Dictionary<string, T>();
        var results = await cache.GetAllAsync<T>(keys.Select(EnsurePrefix), token).ConfigAwait();
        if (results == null) return new Dictionary<string, T>();
        var to = new Dictionary<string, T>();
        foreach (var entry in results)
        {
            to[RemovePrefix(entry.Key)] = entry.Value;
        }
        return to;
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await cache.ReplaceAsync(EnsurePrefix(key), value, expiresIn, token).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await cache.SetAsync(EnsurePrefix(key), value, expiresIn, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await cache.AddAsync(EnsurePrefix(key), value, expiresIn, token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await cache.ReplaceAsync(EnsurePrefix(key), value, expiresAt, token).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await cache.SetAsync(EnsurePrefix(key), value, expiresAt, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await cache.AddAsync(EnsurePrefix(key), value, expiresAt, token).ConfigAwait();
    }

    public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token=default)
    {
        if (keys == null) return;
        await cache.RemoveAllAsync(keys.Select(EnsurePrefix), token).ConfigAwait();
    }

    public async Task FlushAllAsync(CancellationToken token=default)
    {
        // Cannot be prefixed
        await cache.FlushAllAsync(token).ConfigAwait();
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken token=default)
    {
        if (cache is IRemoveByPatternAsync removeByPattern)
            await removeByPattern.RemoveByPatternAsync(EnsurePrefix(pattern), token).ConfigAwait();
        else
            throw new NotImplementedException("IRemoveByPatternAsync is not implemented by: " + cache.GetType().FullName);
    }

    public async Task RemoveByRegexAsync(string regex, CancellationToken token=default)
    {
        if (cache is IRemoveByPatternAsync removeByPattern)
            await removeByPattern.RemoveByRegexAsync(EnsurePrefix(regex), token).ConfigAwait();
        else
            throw new NotImplementedException("IRemoveByPatternAsync is not implemented by: " + cache.GetType().FullName);
    }

#pragma warning disable CS8425
    public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, CancellationToken token = default)
    {
        await foreach (var key in cache.GetKeysByPatternAsync(EnsurePrefix(pattern), token))
        {
            yield return key;
        }
    }
#pragma warning restore CS8425

    public void Dispose()
    {
        (cache as IDisposable)?.Dispose();
    }

    public ValueTask DisposeAsync() => cache.DisposeAsync();

    public async Task RemoveExpiredEntriesAsync(CancellationToken token=default)
    {
        await cache.RemoveExpiredEntriesAsync(token).ConfigAwait();
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token=default)
    {
        return await cache.GetTimeToLiveAsync(EnsurePrefix(key), token).ConfigAwait();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string EnsurePrefix(string s) => s != null && !s.StartsWith(prefix)
        ? prefix + s
        : s;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string RemovePrefix(string s) => s != null && prefix != null && s.StartsWith(prefix)
        ? s.Substring(prefix.Length)
        : s;
        
    public string Prefix => prefix;
}

public static class CacheClientWithPrefixAsyncExtensions
{
    /// <summary>
    /// Decorates the ICacheClient (and its siblings) prefixing every key with the given prefix
    /// 
    /// Useful for multi-tenant environments
    /// </summary>
    public static ICacheClientAsync WithPrefix(this ICacheClientAsync cache, string prefix)
    {
        return new CacheClientWithPrefixAsync(cache, prefix);
    }
}