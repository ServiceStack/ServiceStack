using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Caching;

namespace ServiceStack.Redis
{
    /// <summary>
    /// For interoperability GetCacheClient() and GetReadOnlyCacheClient()
    /// return an ICacheClient wrapper around the redis manager which has the affect of calling 
    /// GetClient() for all write operations and GetReadOnlyClient() for the read ones.
    /// 
    /// This works well for master-replica replication scenarios where you have 
    /// 1 master that replicates to multiple read replicas.
    /// </summary>
    public partial class RedisClientManagerCacheClient : ICacheClient, IRemoveByPattern, ICacheClientExtended
    {
        private readonly IRedisClientsManager redisManager;

        public bool ReadOnly { get; set; }

        public RedisClientManagerCacheClient(IRedisClientsManager redisManager)
        {
            this.redisManager = redisManager;
        }

        /// <summary>
        /// Ignore dispose on RedisClientsManager, which should be registered as a singleton
        /// </summary>
        public void Dispose() { }

        public T Get<T>(string key)
        {
            using var client = redisManager.GetReadOnlyClient();
            return client.Get<T>(key);
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            using var client = redisManager.GetReadOnlyClient();
            return client.GetAll<T>(keys);
        }

        private void AssertNotReadOnly()
        {
            if (this.ReadOnly)
                throw new InvalidOperationException("Cannot perform write operations on a Read-only client");
        }

        public ICacheClient GetClient()
        {
            AssertNotReadOnly();
            return redisManager.GetClient();
        }

        public bool Remove(string key)
        {
            using var client = GetClient();
            return client.Remove(key);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            using var client = GetClient();
            client.RemoveAll(keys);
        }

        public long Increment(string key, uint amount)
        {
            using var client = GetClient();
            return client.Increment(key, amount);
        }

        public long Decrement(string key, uint amount)
        {
            using var client = GetClient();
            return client.Decrement(key, amount);
        }

        public bool Add<T>(string key, T value)
        {
            using var client = GetClient();
            return client.Add(key, value);
        }

        public bool Set<T>(string key, T value)
        {
            using var client = GetClient();
            return client.Set(key, value);
        }

        public bool Replace<T>(string key, T value)
        {
            using var client = GetClient();
            return client.Replace(key, value);
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            using var client = GetClient();
            return client.Add(key, value, expiresAt);
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            using var client = GetClient();
            return client.Set(key, value, expiresAt);
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            using var client = GetClient();
            return client.Replace(key, value, expiresAt);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            using var client = GetClient();
            return client.Set(key, value, expiresIn);
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            using var client = GetClient();
            return client.Set(key, value, expiresIn);
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            using var client = GetClient();
            return client.Replace(key, value, expiresIn);
        }

        public void FlushAll()
        {
            using var client = GetClient();
            client.FlushAll();
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            using var client = GetClient();
            client.SetAll(values);
        }

        public void RemoveByPattern(string pattern)
        {
            using var client = GetClient();
            if (client is IRemoveByPattern redisClient)
            {
                redisClient.RemoveByPattern(pattern);
            }
        }

        public void RemoveByRegex(string pattern)
        {
            using var client = GetClient();
            if (client is IRemoveByPattern redisClient)
            {
                redisClient.RemoveByRegex(pattern);
            }
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            using var client = GetClient();
            if (client is ICacheClientExtended redisClient)
            {
                return redisClient.GetTimeToLive(key);
            }

            return null;
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            using var client = (ICacheClientExtended)GetClient();
            return client.GetKeysByPattern(pattern).ToList();
        }

        public void RemoveExpiredEntries()
        {
            //Redis automatically removed expired Cache Entries
        }
    }
}