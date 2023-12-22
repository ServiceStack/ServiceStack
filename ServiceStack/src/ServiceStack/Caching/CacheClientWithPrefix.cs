using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ServiceStack.Caching;

/// <summary>
/// Decorates the ICacheClient (and its siblings) prefixing every key with the given prefix
/// 
/// Useful for multi-tenant environments
/// </summary>
public class CacheClientWithPrefix : ICacheClient, ICacheClientExtended, IRemoveByPattern
{
    private readonly string prefix;
    private readonly ICacheClient cache;

    public CacheClientWithPrefix(ICacheClient cache, string prefix)
    {
        this.prefix = prefix;
        this.cache = cache;
    }

    public bool Remove(string key)
    {
        return cache.Remove(EnsurePrefix(key));
    }

    public T Get<T>(string key)
    {
        return cache.Get<T>(EnsurePrefix(key));
    }

    public long Increment(string key, uint amount)
    {
        return cache.Increment(EnsurePrefix(key), amount);
    }

    public long Decrement(string key, uint amount)
    {
        return cache.Decrement(EnsurePrefix(key), amount);
    }

    public bool Add<T>(string key, T value)
    {
        return cache.Add(EnsurePrefix(key), value);
    }

    public bool Set<T>(string key, T value)
    {
        return cache.Set(EnsurePrefix(key), value);
    }

    public bool Replace<T>(string key, T value)
    {
        return cache.Replace(EnsurePrefix(key), value);
    }

    public void SetAll<T>(IDictionary<string, T> values)
    {
        cache.SetAll(values.ToDictionary(x => EnsurePrefix(x.Key), x => x.Value));
    }

    public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
    {
        return cache.GetAll<T>(keys.Select(EnsurePrefix));
    }

    public bool Replace<T>(string key, T value, TimeSpan expiresIn)
    {
        return cache.Replace(EnsurePrefix(key), value, expiresIn);
    }

    public bool Set<T>(string key, T value, TimeSpan expiresIn)
    {
        return cache.Set(EnsurePrefix(key), value, expiresIn);
    }

    public bool Add<T>(string key, T value, TimeSpan expiresIn)
    {
        return cache.Add(EnsurePrefix(key), value, expiresIn);
    }

    public bool Replace<T>(string key, T value, DateTime expiresAt)
    {
        return cache.Replace(EnsurePrefix(key), value, expiresAt);
    }

    public bool Set<T>(string key, T value, DateTime expiresAt)
    {
        return cache.Set(EnsurePrefix(key), value, expiresAt);
    }

    public bool Add<T>(string key, T value, DateTime expiresAt)
    {
        return cache.Add(EnsurePrefix(key), value, expiresAt);
    }

    public void RemoveAll(IEnumerable<string> keys)
    {
        cache.RemoveAll(keys.Select(EnsurePrefix));
    }

    public void FlushAll()
    {
        // Cannot be prefixed
        cache.FlushAll();
    }

    public void Dispose()
    {
        cache.Dispose();
    }

    public void RemoveByPattern(string pattern)
    {
        cache.RemoveByPattern(EnsurePrefix(pattern));
    }

    public void RemoveByRegex(string regex)
    {
        cache.RemoveByRegex(EnsurePrefix(regex));
    }

    public IEnumerable<string> GetKeysByPattern(string pattern)
    {
        return cache.GetKeysByPattern(EnsurePrefix(pattern));
    }

    public void RemoveExpiredEntries()
    {
        (cache as ICacheClientExtended)?.RemoveExpiredEntries();
    }

    public TimeSpan? GetTimeToLive(string key)
    {
        return cache.GetTimeToLive(EnsurePrefix(key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string EnsurePrefix(string s) => s != null && !s.StartsWith(prefix)
        ? prefix + s
        : s;
        
    public string Prefix => prefix;
}

public static class CacheClientWithPrefixExtensions
{
    /// <summary>
    /// Decorates the ICacheClient (and its siblings) prefixing every key with the given prefix
    /// 
    /// Useful for multi-tenant environments
    /// </summary>
    public static ICacheClient WithPrefix(this ICacheClient cache, string prefix)
    {
        return new CacheClientWithPrefix(cache, prefix);            
    }
}