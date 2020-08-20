using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Caching
{
    public class CacheClientAsyncWrapper : ICacheClientAsync, IRemoveByPatternAsync
    {
        public ICacheClient Cache { get; }
        public CacheClientAsyncWrapper(ICacheClient cache) => Cache = cache;

        public Task<bool> RemoveAsync(string key) => Cache.Remove(key).InTask();

        public Task RemoveAllAsync(IEnumerable<string> keys)
        {
            Cache.RemoveAll(keys);
            return TypeConstants.EmptyTask;
        }

        public Task<T> GetAsync<T>(string key) => Cache.Get<T>(key).InTask();

        public Task<long> IncrementAsync(string key, uint amount) => Cache.Increment(key, amount).InTask();

        public Task<long> DecrementAsync(string key, uint amount) => Cache.Decrement(key, amount).InTask();

        public Task<bool> AddAsync<T>(string key, T value) => Cache.Add(key, value).InTask();

        public Task<bool> SetAsync<T>(string key, T value) => Cache.Set(key, value).InTask();

        public Task<bool> ReplaceAsync<T>(string key, T value) => Cache.Replace(key, value).InTask();

        public Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt) => Cache.Add(key, value, expiresAt).InTask();

        public Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt) => Cache.Set(key, value, expiresAt).InTask();

        public Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt) => Cache.Replace(key, value, expiresAt).InTask();

        public Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn) => Cache.Add(key, value, expiresIn).InTask();

        public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn) => Cache.Set(key, value, expiresIn).InTask();

        public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn) => Cache.Replace(key, value, expiresIn).InTask();

        public Task FlushAllAsync()
        {
            Cache.FlushAll();
            return TypeConstants.EmptyTask;
        }

        public Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys) => Cache.GetAll<T>(keys).InTask();

        public Task SetAllAsync<T>(IDictionary<string, T> values)
        {
            Cache.SetAll(values);
            return TypeConstants.EmptyTask;
        }

        public Task<TimeSpan?> GetTimeToLiveAsync(string key) => Cache.GetTimeToLive(key).InTask();

        public Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern) => Cache.GetKeysByPattern(pattern).InTask();

        public Task RemoveExpiredEntriesAsync()
        {
            (Cache as ICacheClientExtended)?.RemoveExpiredEntries();
            return TypeConstants.EmptyTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            Cache.RemoveByPattern(pattern);
            return TypeConstants.EmptyTask;
        }

        public Task RemoveByRegexAsync(string regex)
        {
            Cache.RemoveByRegex(regex);
            return TypeConstants.EmptyTask;
        }
    }

    public static class CacheClientAsyncExtensions
    {
        public static ICacheClientAsync AsAsync(this ICacheClient cache) =>
            cache as ICacheClientAsync ?? new CacheClientAsyncWrapper(cache);
    }
}