using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Caching
{
    /// <summary>
    /// Decorates the ICacheClient (and its sibblings) prefixing every key with the given prefix
    /// 
    /// Usefull for multi-tenant environments
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
            return cache.Remove(prefix + key);
        }

        public T Get<T>(string key)
        {
            return cache.Get<T>(prefix + key);
        }

        public long Increment(string key, uint amount)
        {
            return cache.Increment(prefix + key, amount);
        }

        public long Decrement(string key, uint amount)
        {
            return cache.Decrement(prefix + key, amount);
        }

        public bool Add<T>(string key, T value)
        {
            return cache.Add(prefix + key, value);
        }

        public bool Set<T>(string key, T value)
        {
            return cache.Set(prefix + key, value);
        }

        public bool Replace<T>(string key, T value)
        {
            return cache.Replace(prefix + key, value);
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            cache.SetAll(values.ToDictionary(x => prefix + x.Key, x => x.Value));
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return cache.GetAll<T>(keys.Select(x => prefix + x));
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return cache.Replace(prefix + key, value, expiresIn);
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return cache.Set(prefix + key, value, expiresIn);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return cache.Add(prefix + key, value, expiresIn);
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return cache.Replace(prefix + key, value, expiresAt);
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return cache.Set(prefix + key, value, expiresAt);
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return cache.Add(prefix + key, value, expiresAt);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            cache.RemoveAll(keys.Select(x => prefix + x));
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
            (cache as IRemoveByPattern)?.RemoveByPattern(prefix + pattern);
        }

        public void RemoveByRegex(string regex)
        {
            (cache as IRemoveByPattern)?.RemoveByRegex(prefix + regex);
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            return (cache as ICacheClientExtended)?.GetKeysByPattern(prefix + pattern);
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            return (cache as ICacheClientExtended)?.GetTimeToLive(key);
        }
        
        public string Prefix => prefix;
    }

    public static class CacheClientWithPrefixExtensions
    {
        /// <summary>
        /// Decorates the ICacheClient (and its sibblings) prefixing every key with the given prefix
        /// 
        /// Usefull for multi-tenant environments
        /// </summary>
        public static ICacheClient WithPrefix(this ICacheClient cache, string prefix)
        {
            return new CacheClientWithPrefix(cache, prefix);            
        }
    }

}
