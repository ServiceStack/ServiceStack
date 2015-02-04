using System;
using System.Collections.Generic;
using Microsoft.ApplicationServer.Caching;
using ServiceStack.Logging;

namespace ServiceStack.Caching.Azure
{
    public class AzureCacheClient : AdapterBase, ICacheClient
    {
        private DataCacheFactory CacheFactory { get; set; }
        private DataCache DataCache { get; set; }
        public bool FlushOnDispose { get; set; }
        protected override ILog Log { get { return LogManager.GetLogger(GetType()); } }

        public AzureCacheClient(string cacheName = null)
        {
            CacheFactory = new DataCacheFactory();
            if (string.IsNullOrEmpty(cacheName))
                DataCache = CacheFactory.GetDefaultCache();
            else
                DataCache = CacheFactory.GetCache(cacheName);
        }

        private bool TryGetValue(string key, out object entry)
        {
            entry = DataCache.Get(key);
            return entry != null;
        }

        private bool CacheAdd(string key, object value)
        {
            return CacheAdd(key, value, DateTime.MaxValue);
        }

        /// <summary>
        /// Stores The value with key only if such key doesn't exist at the server yet. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiresAt">The expires at.</param>
        /// <returns></returns>
        private bool CacheAdd(string key, object value, DateTime expiresAt)
        {
            object entry;
            if (TryGetValue(key, out entry)) return false;
            DataCache.Add(key, value, expiresAt.Subtract(DateTime.Now));
            return true;
        }

        private bool CacheSet(string key, object value)
        {
            return CacheSet(key, value, DateTime.MaxValue);
        }

        /// <summary>
        /// Adds or replaces the value with key. Return false if a version exists but is not the lastversion.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiresAt">The expires at.</param>
        /// <param name="checkLastVersion"> The check last version</param>
        /// <returns>True; if it succeeded</returns>        
        private bool CacheSet(string key, object value, DateTime expiresAt, DataCacheItemVersion checkLastVersion = null)
        {
            if (checkLastVersion != null)
            {
                object entry = DataCache.GetIfNewer(key, ref checkLastVersion);
                if (entry != null)
                {
                    //update value and version
                    DataCache.Put(key, value, checkLastVersion, expiresAt.Subtract(DateTime.Now));
                    return true;
                }
                if (TryGetValue(key, out entry))
                {//version exists but is older.
                    return false;
                }
            }
            //if we don't care about version, then just update
            DataCache.Put(key, value, expiresAt.Subtract(DateTime.Now));
            return true;
        }

        private bool CacheReplace(string key, object value)
        {
            return CacheReplace(key, value, DateTime.MaxValue);
        }

        private bool CacheReplace(string key, object value, DateTime expiresAt)
        {
            return !CacheSet(key, value, expiresAt); ;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (!FlushOnDispose) return;

            FlushAll();
        }

        /// <summary>
        /// Removes the specified item from the cache.
        /// </summary>
        /// <param name="key">The identifier for the item to delete.</param>
        /// <returns>
        /// true if the item was successfully removed from the cache; false otherwise.
        /// </returns>
        public bool Remove(string key)
        {
            return DataCache.Remove(key);
        }

        /// <summary>
        /// Removes the cache for all the keys provided.
        /// </summary>
        /// <param name="keys">The keys.</param>
        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                try
                {
                    Remove(key);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Error trying to remove {0} from azure cache", key), ex);
                }
            }
        }

        public object Get(string key)
        {
            DataCacheItemVersion version;
            return Get(key, out version);
        }

        public object Get(string key, out DataCacheItemVersion version)
        {
            return DataCache.Get(key, out version);
        }

        /// <summary>
        /// Retrieves the specified item from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The identifier for the item to retrieve.</param>
        /// <returns>
        /// The retrieved item, or <value>null</value> if the key was not found.
        /// </returns>
        public T Get<T>(string key)
        {
            var value = Get(key);
            if (value != null) return (T)value;
            return default(T);
        }

        /// <summary>
        /// Increments the value of the specified key by the given amount. 
        /// The operation is atomic and happens on the server.
        /// A non existent value at key starts at 0
        /// </summary>
        /// <param name="key">The identifier for the item to increment.</param>
        /// <param name="amount">The amount by which the client wants to increase the item.</param>
        /// <returns>
        /// The new value of the item or -1 if not found.
        /// </returns>
        /// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
        public long Increment(string key, uint amount)
        {
            return UpdateCounter(key, (int)amount);
        }

        private long UpdateCounter(string key, int value)
        {
            long longVal;
            if (Int64.TryParse(Get(key).ToString(), out longVal))
            {
                longVal += value;
                CacheSet(key, longVal);
                return longVal;
            }
            CacheSet(key, 0);
            return 0;
        }

        /// <summary>
        /// Increments the value of the specified key by the given amount. 
        /// The operation is atomic and happens on the server.
        /// A non existent value at key starts at 0
        /// </summary>
        /// <param name="key">The identifier for the item to increment.</param>
        /// <param name="amount">The amount by which the client wants to decrease the item.</param>
        /// <returns>
        /// The new value of the item or -1 if not found.
        /// </returns>
        /// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
        public long Decrement(string key, uint amount)
        {
            return UpdateCounter(key, (int)-amount);
        }

        /// <summary>
        /// Adds a new item into the cache at the specified cache key only if the cache is empty.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <returns>
        /// true if the item was successfully stored in the cache; false otherwise.
        /// </returns>
        /// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
        public bool Add<T>(string key, T value)
        {
            return CacheAdd(key, value);
        }

        /// <summary>
        /// Sets an item into the cache at the cache key specified regardless if it already exists or not.
        /// </summary>
        public bool Set<T>(string key, T value)
        {
            return CacheSet(key, value);
        }

        /// <summary>
        /// Replaces the item at the cachekey specified only if an items exists at the location already. 
        /// </summary>
        public bool Replace<T>(string key, T value)
        {
            return CacheReplace(key, value);
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return CacheAdd(key, value, expiresAt);
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return CacheSet(key, value, expiresAt);
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return CacheReplace(key, value, expiresAt);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheAdd(key, value, DateTime.Now.Add(expiresIn));
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheSet(key, value, DateTime.Now.Add(expiresIn));
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheReplace(key, value, DateTime.Now.Add(expiresIn));
        }

        /// <summary>
        /// Invalidates all data on the cache.
        /// </summary>
        public void FlushAll()
        {
            var regions = DataCache.GetSystemRegions();
            foreach (var region in regions)
            {
                DataCache.ClearRegion(region);
            }
        }

        /// <summary>
        /// Retrieves multiple items from the cache. 
        /// The default value of T is set for all keys that do not exist.
        /// </summary>
        /// <param name="keys">The list of identifiers for the items to retrieve.</param>
        /// <returns>
        /// a Dictionary holding all items indexed by their key.
        /// </returns>
        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var value = Get<T>(key);
                valueMap[key] = value;
            }
            return valueMap;
        }

        /// <summary>
        /// Sets multiple items to the cache. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values">The values.</param>
        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }
    }
}
