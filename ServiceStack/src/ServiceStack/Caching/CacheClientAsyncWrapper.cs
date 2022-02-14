using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Caching
{
    public class CacheClientAsyncWrapper : ICacheClientAsync, IRemoveByPatternAsync
    {
        public ICacheClient Cache { get; }
        public CacheClientAsyncWrapper(ICacheClient cache) => Cache = cache;

        public Task<bool> RemoveAsync(string key, CancellationToken token=default) => Cache.Remove(key).InTask();

        public Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token=default)
        {
            Cache.RemoveAll(keys);
            return TypeConstants.EmptyTask;
        }

        public Task<T> GetAsync<T>(string key, CancellationToken token=default) => Cache.Get<T>(key).InTask();

        public Task<long> IncrementAsync(string key, uint amount, CancellationToken token=default) => Cache.Increment(key, amount).InTask();

        public Task<long> DecrementAsync(string key, uint amount, CancellationToken token=default) => Cache.Decrement(key, amount).InTask();

        public Task<bool> AddAsync<T>(string key, T value, CancellationToken token=default) => Cache.Add(key, value).InTask();

        public Task<bool> SetAsync<T>(string key, T value, CancellationToken token=default) => Cache.Set(key, value).InTask();

        public Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token=default) => Cache.Replace(key, value).InTask();

        public Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default) => Cache.Add(key, value, expiresAt).InTask();

        public Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default) => Cache.Set(key, value, expiresAt).InTask();

        public Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default) => Cache.Replace(key, value, expiresAt).InTask();

        public Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default) => Cache.Add(key, value, expiresIn).InTask();

        public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default) => Cache.Set(key, value, expiresIn).InTask();

        public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default) => Cache.Replace(key, value, expiresIn).InTask();

        public Task FlushAllAsync(CancellationToken token=default)
        {
            Cache.FlushAll();
            return TypeConstants.EmptyTask;
        }

        public Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token=default) => Cache.GetAll<T>(keys).InTask();

        public Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token=default)
        {
            Cache.SetAll(values);
            return TypeConstants.EmptyTask;
        }

        public Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token=default) => Cache.GetTimeToLive(key).InTask();
        
        public Task RemoveExpiredEntriesAsync(CancellationToken token=default)
        {
            (Cache as ICacheClientExtended)?.RemoveExpiredEntries();
            return TypeConstants.EmptyTask;
        }

        public Task RemoveByPatternAsync(string pattern, CancellationToken token=default)
        {
            Cache.RemoveByPattern(pattern);
            return TypeConstants.EmptyTask;
        }

        public Task RemoveByRegexAsync(string regex, CancellationToken token=default)
        {
            Cache.RemoveByRegex(regex);
            return TypeConstants.EmptyTask;
        }

#pragma warning disable CS1998
        public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, [EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var key in Cache.GetKeysByPattern(pattern))
            {
                token.ThrowIfCancellationRequested();
                
                yield return key;
            }
        }
#pragma warning restore CS1998

        public ValueTask DisposeAsync()
        {
            Cache.Dispose();
            return default;
        }
    }

    public static class CacheClientAsyncExtensions
    {
        public static ICacheClientAsync AsAsync(this ICacheClient cache) =>
            cache as ICacheClientAsync ?? new CacheClientAsyncWrapper(cache);

        /// <summary>
        /// Returns underlying wrapped sync ICacheClient or ICacheClient API if cache implements it  
        /// </summary>
        public static ICacheClient AsSync(this ICacheClientAsync cache) => cache is CacheClientAsyncWrapper wrapper
            ? wrapper.Cache
            : cache as ICacheClient;

        /// <summary>
        /// Returns sync ICacheClient if wrapped  
        /// </summary>
        public static ICacheClient Unwrap(this ICacheClientAsync cache) => cache is CacheClientAsyncWrapper wrapper
            ? wrapper.Cache
            : null;
    }
}