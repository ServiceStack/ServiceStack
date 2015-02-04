using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ServiceStack.Logging;

namespace ServiceStack.Caching
{
    public class MemoryCacheClient : ICacheClientExtended, IRemoveByPattern
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryCacheClient));

        private ConcurrentDictionary<string, CacheEntry> memory;
        private ConcurrentDictionary<string, int> counters;

        public bool FlushOnDispose { get; set; }

        private class CacheEntry
        {
            private object cacheValue;

            /// <summary>
            /// Create new instance of CacheEntry.
            /// </summary>
            public CacheEntry(object value, DateTime? expiresAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
                LastModifiedTicks = DateTime.UtcNow.Ticks;
            }

            /// <summary>UTC time at which CacheEntry expires.</summary>
            internal DateTime? ExpiresAt { get; set; }

            internal bool HasExpired
            {
                get { return ExpiresAt != null && ExpiresAt < DateTime.UtcNow; }
            }

            internal object Value
            {
                get { return cacheValue; }
                set
                {
                    cacheValue = value;
                    LastModifiedTicks = DateTime.UtcNow.Ticks;
                }
            }

            internal long LastModifiedTicks { get; private set; }
        }

        public MemoryCacheClient()
        {
            this.memory = new ConcurrentDictionary<string, CacheEntry>();
            this.counters = new ConcurrentDictionary<string, int>();
        }

        private bool TryGetValue(string key, out CacheEntry entry)
        {
            return this.memory.TryGetValue(key, out entry);
        }

        private void Set(string key, CacheEntry entry)
        {
            this.memory[key] = entry;
        }

        /// <summary>
        /// Stores The value with key only if such key doesn't exist at the server yet. 
        /// </summary>
        private bool CacheAdd(string key, object value, DateTime? expiresAt = null)
        {
            CacheEntry entry;
            if (this.TryGetValue(key, out entry)) return false;

            entry = new CacheEntry(value, expiresAt);
            this.Set(key, entry);

            return true;
        }

        /// <summary>
        /// Adds or replaces the value with key.
        /// </summary>
        private bool CacheSet(string key, object value, DateTime expiresAt)
        {
            return CacheSet(key, value, expiresAt, null);
        }

        /// <summary>
        /// Adds or replaces the value with key. 
        /// </summary>
        private bool CacheSet(string key, object value, DateTime? expiresAt = null, long? checkLastModified = null)
        {
            CacheEntry entry;
            if (!this.TryGetValue(key, out entry))
            {
                entry = new CacheEntry(value, expiresAt);
                this.Set(key, entry);
                return true;
            }

            if (checkLastModified.HasValue
                && entry.LastModifiedTicks != checkLastModified.Value) return false;

            entry.Value = value;
            entry.ExpiresAt = expiresAt;

            return true;
        }

        /// <summary>
        /// Replace the value with specified key if it exists.
        /// </summary>
        private bool CacheReplace(string key, object value, DateTime? expiresAt = null)
        {
            return !CacheSet(key, value, expiresAt);
        }

        public void Dispose()
        {
            if (!FlushOnDispose) return;

            this.memory = new ConcurrentDictionary<string, CacheEntry>();
            this.counters = new ConcurrentDictionary<string, int>();
        }

        public bool Remove(string key)
        {
            CacheEntry item;
            return this.memory.TryRemove(key, out item);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                try
                {
                    this.Remove(key);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Error trying to remove {0} from the cache", key), ex);
                }
            }
        }

        public object Get(string key)
        {
            long lastModifiedTicks;
            return Get(key, out lastModifiedTicks);
        }

        public object Get(string key, out long lastModifiedTicks)
        {
            lastModifiedTicks = 0;

            CacheEntry cacheEntry;
            if (this.memory.TryGetValue(key, out cacheEntry))
            {
                if (cacheEntry.HasExpired)
                {
                    this.memory.TryRemove(key, out cacheEntry);
                    return null;
                }
                lastModifiedTicks = cacheEntry.LastModifiedTicks;
                return cacheEntry.Value;
            }
            return null;
        }

        public T Get<T>(string key)
        {
            var value = Get(key);
            if (value != null) return (T)value;
            return default(T);
        }

        private int UpdateCounter(string key, int value)
        {
            if (!this.counters.ContainsKey(key))
            {
                this.counters[key] = 0;
            }
            this.counters[key] += value;
            return this.counters[key];
        }

        public long Increment(string key, uint amount)
        {
            return UpdateCounter(key, (int)amount);
        }

        public long Decrement(string key, uint amount)
        {
            return UpdateCounter(key, (int)amount * -1);
        }

        /// <summary>
        /// Add the value with key to the cache, set to never expire.
        /// </summary>
        public bool Add<T>(string key, T value)
        {
            return CacheAdd(key, value);
        }

        /// <summary>
        /// Add or replace the value with key to the cache, set to never expire.
        /// </summary>
        public bool Set<T>(string key, T value)
        {
            return CacheSet(key, value);
        }

        /// <summary>
        /// Replace the value with key in the cache, set to never expire.
        /// </summary>
        public bool Replace<T>(string key, T value)
        {
            return CacheReplace(key, value);
        }

        /// <summary>
        /// Add the value with key to the cache, set to expire at specified DateTime.
        /// </summary>
        /// <remarks>This method examines the DateTimeKind of expiresAt to determine if conversion to
        /// universal time is needed. The version of Add that takes a TimeSpan expiration is faster 
        /// than using this method with a DateTime of Kind other than Utc, and is not affected by 
        /// ambiguous local time during daylight savings/standard time transition.</remarks>
        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            if (expiresAt.Kind != DateTimeKind.Utc) expiresAt = expiresAt.ToUniversalTime();
            return CacheAdd(key, value, expiresAt);
        }

        /// <summary>
        /// Add or replace the value with key to the cache, set to expire at specified DateTime.
        /// </summary>
        /// <remarks>This method examines the DateTimeKind of expiresAt to determine if conversion to
        /// universal time is needed. The version of Set that takes a TimeSpan expiration is faster 
        /// than using this method with a DateTime of Kind other than Utc, and is not affected by 
        /// ambiguous local time during daylight savings/standard time transition.</remarks>
        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            if (expiresAt.Kind != DateTimeKind.Utc) expiresAt = expiresAt.ToUniversalTime();
            return CacheSet(key, value, expiresAt);
        }

        /// <summary>
        /// Replace the value with key in the cache, set to expire at specified DateTime.
        /// </summary>
        /// <remarks>This method examines the DateTimeKind of expiresAt to determine if conversion to
        /// universal time is needed. The version of Replace that takes a TimeSpan expiration is faster 
        /// than using this method with a DateTime of Kind other than Utc, and is not affected by 
        /// ambiguous local time during daylight savings/standard time transition.</remarks>
        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            if (expiresAt.Kind != DateTimeKind.Utc) expiresAt = expiresAt.ToUniversalTime();
            return CacheReplace(key, value, expiresAt);
        }

        /// <summary>
        /// Add the value with key to the cache, set to expire after specified TimeSpan.
        /// </summary>
        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheAdd(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        /// <summary>
        /// Add or replace the value with key to the cache, set to expire after specified TimeSpan.
        /// </summary>
        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheSet(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        /// <summary>
        /// Replace the value with key in the cache, set to expire after specified TimeSpan.
        /// </summary>
        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheReplace(key, value, DateTime.UtcNow.Add(expiresIn));
        }

        public void FlushAll()
        {
            this.memory = new ConcurrentDictionary<string, CacheEntry>();
        }

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

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }

        public void RemoveByPattern(string pattern)
        {
            RemoveByRegex(pattern.Replace("*", ".*").Replace("?", ".+"));
        }

        public void RemoveByRegex(string pattern)
        {
            var regex = new Regex(pattern);
            var enumerator = this.memory.GetEnumerator();
            var keysToRemove = new List<string>();
            try
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (regex.IsMatch(current.Key) || current.Value.HasExpired)
                    {
                        keysToRemove.Add(current.Key);
                    }
                }
                RemoveAll(keysToRemove);
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Error trying to remove items from cache with this {0} pattern", pattern), ex);
            }
        }

        public List<string> GetKeysByPattern(string pattern)
        {
            var regex = new Regex(pattern);
            var enumerator = this.memory.GetEnumerator();
            var keys = new List<string>();
            var expiredKeys = new List<string>();
            try
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (!regex.IsMatch(current.Key))
                        continue;

                    if (current.Value.HasExpired)
                    {
                        expiredKeys.Add(current.Key);
                    }
                    else
                    {
                        keys.Add(current.Key);
                    }
                }

                RemoveAll(expiredKeys);
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Error trying to remove items from cache with this {0} pattern", pattern), ex);
            }
            return keys;
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            CacheEntry cacheEntry;
            if (this.memory.TryGetValue(key, out cacheEntry))
            {
                if (cacheEntry.ExpiresAt == null)
                    return TimeSpan.MaxValue;

                return cacheEntry.ExpiresAt - DateTime.UtcNow;
            }
            return null;
        }
    }
}