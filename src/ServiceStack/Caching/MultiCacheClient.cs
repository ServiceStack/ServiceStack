using System;
using System.Collections.Generic;
using ServiceStack.Common;

namespace ServiceStack.Caching
{
    public class MultiCacheClient
        : ICacheClient
    {
        private readonly List<ICacheClient> cacheClients;

        public MultiCacheClient(params ICacheClient[] cacheClients)
        {
            if (cacheClients.Length == 0)
                throw new ArgumentNullException(nameof(cacheClients));

            this.cacheClients = new List<ICacheClient>(cacheClients);
        }

        public void Dispose()
        {
            cacheClients.ExecAll(client => client.Dispose());
        }

        public bool Remove(string key)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Remove(key), ref firstResult);
            return firstResult;
        }

        public T Get<T>(string key)
        {
            return cacheClients.ExecReturnFirstWithResult(client => client.Get<T>(key));
        }

        public long Increment(string key, uint amount)
        {
            var firstResult = default(long);
            cacheClients.ExecAllWithFirstOut(client => client.Increment(key, amount), ref firstResult);
            return firstResult;
        }

        public long Decrement(string key, uint amount)
        {
            var firstResult = default(long);
            cacheClients.ExecAllWithFirstOut(client => client.Decrement(key, amount), ref firstResult);
            return firstResult;
        }

        public bool Add<T>(string key, T value)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Add(key, value), ref firstResult);
            return firstResult;
        }

        public bool Set<T>(string key, T value)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Set(key, value), ref firstResult);
            return firstResult;
        }

        public bool Replace<T>(string key, T value)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Replace(key, value), ref firstResult);
            return firstResult;
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Add(key, value, expiresAt), ref firstResult);
            return firstResult;
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Set(key, value, expiresAt), ref firstResult);
            return firstResult;
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Replace(key, value, expiresAt), ref firstResult);
            return firstResult;
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Add(key, value, expiresIn), ref firstResult);
            return firstResult;
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Set(key, value, expiresIn), ref firstResult);
            return firstResult;
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            var firstResult = default(bool);
            cacheClients.ExecAllWithFirstOut(client => client.Replace(key, value, expiresIn), ref firstResult);
            return firstResult;
        }

        public void FlushAll()
        {
            cacheClients.ExecAll(client => client.FlushAll());
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            foreach (var client in cacheClients)
            {
                try
                {
                    var result = client.GetAll<T>(keys);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    ExecExtensions.LogError(client.GetType(), "Get", ex);
                }
            }

            return new Dictionary<string, T>();
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                this.Remove(key);
            }
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }
    }

}