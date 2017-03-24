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
    public class CacheClientPrefixer : ICacheClient, ICacheClientExtended, IRemoveByPattern
    {
        private readonly string _prefix;
        private readonly ICacheClient _cacheClient;

        public CacheClientPrefixer(string prefix, ICacheClient cacheClient)
        {
            _prefix = prefix;
            _cacheClient = cacheClient;
        }

        public bool Remove(string key)
        {
            return _cacheClient.Remove(_prefix + key);
        }

        public T Get<T>(string key)
        {
            return _cacheClient.Get<T>(_prefix + key);
        }

        public long Increment(string key, uint amount)
        {
            return _cacheClient.Increment(_prefix + key, amount);
        }

        public long Decrement(string key, uint amount)
        {
            return _cacheClient.Decrement(_prefix + key, amount);
        }

        public bool Add<T>(string key, T value)
        {
            return _cacheClient.Add(_prefix + key, value);
        }

        public bool Set<T>(string key, T value)
        {
            return _cacheClient.Set(_prefix + key, value);
        }

        public bool Replace<T>(string key, T value)
        {
            return _cacheClient.Replace(_prefix + key, value);
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            _cacheClient.SetAll(values.ToDictionary(x => _prefix + x.Key, x => x.Value));
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return _cacheClient.GetAll<T>(keys.Select(x => _prefix + x));
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return _cacheClient.Replace(_prefix + key, value, expiresIn);
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return _cacheClient.Set(_prefix + key, value, expiresIn);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return _cacheClient.Add(_prefix + key, value, expiresIn);
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return _cacheClient.Replace(_prefix + key, value, expiresAt);
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return _cacheClient.Set(_prefix + key, value, expiresAt);
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return _cacheClient.Add(_prefix + key, value, expiresAt);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            _cacheClient.RemoveAll(keys.Select(x => _prefix + x));
        }

        public void FlushAll()
        {
            // Cannot be prefixed
            _cacheClient.FlushAll();
        }

        public void Dispose()
        {
            _cacheClient.Dispose();
        }

        public void RemoveByPattern(string pattern)
        {
            (_cacheClient as IRemoveByPattern)?.RemoveByPattern(_prefix + pattern);
        }

        public void RemoveByRegex(string regex)
        {
            (_cacheClient as IRemoveByPattern)?.RemoveByRegex(_prefix + regex);
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            return (_cacheClient as ICacheClientExtended)?.GetKeysByPattern(_prefix + pattern);
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            return (_cacheClient as ICacheClientExtended)?.GetTimeToLive(key);
        }
    }
}
