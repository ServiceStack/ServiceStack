using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ServiceStack.Caching
{
    /// <summary>
    /// Decorates the ICacheClient (and its siblings) prefixing every key with the given prefix
    /// 
    /// Useful for multi-tenant environments
    /// </summary>
    public class CacheClientWithPrefixAsync : ICacheClientAsync, IRemoveByPatternAsync
    {
        private readonly string prefix;
        private readonly ICacheClientAsync cache;

        public CacheClientWithPrefixAsync(ICacheClientAsync cache, string prefix)
        {
            this.prefix = prefix;
            this.cache = cache;
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return await cache.RemoveAsync(EnsurePrefix(key));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            return await cache.GetAsync<T>(EnsurePrefix(key));
        }

        public async Task<long> IncrementAsync(string key, uint amount)
        {
            return await cache.IncrementAsync(EnsurePrefix(key), amount);
        }

        public async Task<long> DecrementAsync(string key, uint amount)
        {
            return await cache.DecrementAsync(EnsurePrefix(key), amount);
        }

        public async Task<bool> AddAsync<T>(string key, T value)
        {
            return await cache.AddAsync(EnsurePrefix(key), value);
        }

        public async Task<bool> SetAsync<T>(string key, T value)
        {
            return await cache.SetAsync(EnsurePrefix(key), value);
        }

        public async Task<bool> ReplaceAsync<T>(string key, T value)
        {
            return await cache.ReplaceAsync(EnsurePrefix(key), value);
        }

        public async Task SetAllAsync<T>(IDictionary<string, T> values)
        {
            await cache.SetAllAsync(values.ToDictionary(x => EnsurePrefix(x.Key), x => x.Value));
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys)
        {
            return await cache.GetAllAsync<T>(keys.Select(EnsurePrefix));
        }

        public async Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            return await cache.ReplaceAsync(EnsurePrefix(key), value, expiresIn);
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            return await cache.SetAsync(EnsurePrefix(key), value, expiresIn);
        }

        public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            return await cache.AddAsync(EnsurePrefix(key), value, expiresIn);
        }

        public async Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt)
        {
            return await cache.ReplaceAsync(EnsurePrefix(key), value, expiresAt);
        }

        public async Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt)
        {
            return await cache.SetAsync(EnsurePrefix(key), value, expiresAt);
        }

        public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt)
        {
            return await cache.AddAsync(EnsurePrefix(key), value, expiresAt);
        }

        public async Task RemoveAllAsync(IEnumerable<string> keys)
        {
            await cache.RemoveAllAsync(keys.Select(EnsurePrefix));
        }

        public async Task FlushAllAsync()
        {
            // Cannot be prefixed
            await cache.FlushAllAsync();
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            await (cache as IRemoveByPatternAsync).RemoveByPatternAsync(EnsurePrefix(pattern));
        }

        public async Task RemoveByRegexAsync(string regex)
        {
            (cache as IRemoveByPatternAsync).RemoveByRegexAsync(EnsurePrefix(regex));
        }

        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
        {
            return await cache.GetKeysByPatternAsync(EnsurePrefix(pattern));
        }

        public async Task RemoveExpiredEntriesAsync()
        {
            await cache.RemoveExpiredEntriesAsync();
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            return await cache.GetTimeToLiveAsync(EnsurePrefix(key));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string EnsurePrefix(string s) => s != null && !s.StartsWith(prefix)
            ? prefix + s
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

}
