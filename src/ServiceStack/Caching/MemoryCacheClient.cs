using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Caching
{
    public class MemoryCacheClient : ICacheClientExtended, IRemoveByPattern
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryCacheClient));

        private long hitCounter = 0;

        public long CleaningInterval { get; set; } = 1000;

        private ConcurrentDictionary<string, CacheEntry> memory;
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

            internal bool HasExpired => ExpiresAt != null && ExpiresAt < DateTime.UtcNow;

            internal object Value
            {
                get => cacheValue;
                set
                {
                    cacheValue = value;
                    LastModifiedTicks = DateTime.UtcNow.Ticks;
                }
            }

            internal long LastModifiedTicks { get; private set; }

            protected bool Equals(CacheEntry other)
            {
                return Equals(cacheValue, other.cacheValue);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((CacheEntry) obj);
            }

            public override int GetHashCode()
            {
                return (cacheValue != null ? cacheValue.GetHashCode() : 0);
            }
        }

        public MemoryCacheClient()
        {
            this.memory = new ConcurrentDictionary<string, CacheEntry>();
        }

        private bool TryGetValue(string key, out CacheEntry entry)
        {
            IncrHit();
            return this.memory.TryGetValue(key, out entry);
        }

        private void Set(string key, CacheEntry entry)
        {
            this.memory[key] = entry;
            IncrHit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IncrHit()
        {
            if (Interlocked.Increment(ref hitCounter) % CleaningInterval == 0)
            {
                this.RemoveExpiredEntries();
            }
        }

        /// <summary>
        /// Stores The value with key only if such key doesn't exist at the server yet. 
        /// <returns>true if added</returns>
        /// </summary>
        private bool CacheAdd(string key, object value, DateTime? expiresAt = null)
        {
            IncrHit();
            return this.memory.TryAdd(key, new CacheEntry(value, expiresAt));
        }

        /// <summary>
        /// Adds or replaces the value with key.
        /// <returns>true if added</returns>
        /// </summary>
        private bool CacheSet(string key, object value, DateTime? expiresAt = null)
        {
            IncrHit();
            this.memory[key] = new CacheEntry(value, expiresAt);
            return true;
        }

        /// <summary>
        /// Replace the value with specified key only if it exists.
        /// <returns>true if updated</returns>
        /// </summary>
        private bool CacheReplace(string key, object value, DateTime? expiresAt = null)
        {
            if (this.TryGetValue(key, out var entry))
            {
                lock (entry)
                {
                    entry.Value = value;
                    entry.ExpiresAt = expiresAt;
                }
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (!FlushOnDispose) return;

            this.memory = new ConcurrentDictionary<string, CacheEntry>();
        }

        public bool Remove(string key)
        {
            return this.memory.TryRemove(key, out _);
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
                    Log.Error($"Error trying to remove {key} from the cache", ex);
                }
            }
        }

        public object Get(string key)
        {
            return Get(key, out _);
        }

        public object Get(string key, out long lastModifiedTicks)
        {
            lastModifiedTicks = 0;
            if (this.TryGetValue(key, out var cacheEntry))
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

        private long UpdateCounter(string key, long value)
        {
            IncrHit();
            var currVal = value;
            this.memory.AddOrUpdate(key, new CacheEntry(value, null), (k, existingEntry) => {
                var int64 = Convert.ToInt64(existingEntry.Value);
                return new CacheEntry(currVal = int64 + value, null);
            });
            return currVal;
        }

        public long Increment(string key, uint amount)
        {
            return UpdateCounter(key, amount);
        }

        public long Decrement(string key, uint amount)
        {
            return UpdateCounter(key, amount * -1);
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

        private static string ConvertToRegex(string pattern)
        {
            return pattern.Replace("*", ".*").Replace("?", ".+");
        }

        public void RemoveByPattern(string pattern)
        {
            RemoveByRegex(ConvertToRegex(pattern));
        }

        public void RemoveByRegex(string pattern)
        {
            var regex = new Regex(pattern);
            using (var enumerator = this.memory.GetEnumerator())
            {
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
                    Log.Error($"Error trying to remove items from cache with this {pattern} pattern", ex);
                }
            }
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            return pattern == "*" 
                ? memory.Keys 
                : GetKeysByRegex(ConvertToRegex(pattern));
        }

        public List<string> GetKeysByRegex(string pattern)
        {
            var regex = new Regex(pattern);
            using (var enumerator = this.memory.GetEnumerator())
            {
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
                    Log.Error($"Error trying to remove items from cache with this {pattern} pattern", ex);
                }
                return keys;
            }
        }

        public void RemoveExpiredEntries()
        {
            var expiredKeys = new List<string>();
            using var enumerator = this.memory.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (current.Value.HasExpired)
                {
                    expiredKeys.Add(current.Key);
                }
            }

            RemoveAll(expiredKeys);
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            if (this.TryGetValue(key, out var cacheEntry))
            {
                if (cacheEntry.ExpiresAt == null)
                    return TimeSpan.MaxValue;

                return cacheEntry.ExpiresAt - DateTime.UtcNow;
            }
            return null;
        }
    }
}