using System;
using System.Collections.Generic;
using ServiceStack.Logging;
using ServiceStack.Net30.Collections.Concurrent;

namespace ServiceStack.CacheAccess.Providers
{
	public class MemoryCacheClient 
		: ICacheClient
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (MemoryCacheClient));

		private ConcurrentDictionary<string, CacheEntry> memory;
		private ConcurrentDictionary<string, int> counters;

		public bool FlushOnDispose { get; set; }

		private class CacheEntry
		{
			private object cacheValue;

			public CacheEntry(object value, DateTime expiresAt)
			{
				Value = value;
				ExpiresAt = expiresAt;
				LastModifiedTicks = DateTime.Now.Ticks;
			}

			internal DateTime ExpiresAt { get; set; }
			
			internal object Value
			{
				get { return cacheValue; }
				set
				{
					cacheValue = value;
					LastModifiedTicks = DateTime.Now.Ticks;
				}
			}

			internal long LastModifiedTicks { get; private set; }
		}

		public MemoryCacheClient()
		{
			this.memory = new ConcurrentDictionary<string, CacheEntry>();
			this.counters = new ConcurrentDictionary<string, int>();
		}

		private bool CacheAdd(string key, object value)
		{
			return CacheAdd(key, value, DateTime.MaxValue);
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
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <param name="expiresAt">The expires at.</param>
		/// <returns></returns>
		private bool CacheAdd(string key, object value, DateTime expiresAt)
		{
			CacheEntry entry;
			if (this.TryGetValue(key, out entry)) return false;

			entry = new CacheEntry(value, expiresAt);
			this.Set(key, entry);

			return true;
		}

		private bool CacheSet(string key, object value)
		{
			return CacheSet(key, value, DateTime.MaxValue);
		}

		private bool CacheSet(string key, object value, DateTime expiresAt)
		{
			return CacheSet(key, value, expiresAt, null);
		}

		/// <summary>
		/// Adds or replaces the value with key. 
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <param name="expiresAt">The expires at.</param>
		/// <param name="checkLastModified">The check last modified.</param>
		/// <returns>True; if it succeeded</returns>
		private bool CacheSet(string key, object value, DateTime expiresAt, long? checkLastModified)
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

		private bool CacheReplace(string key, object value)
		{
			return CacheReplace(key, value, DateTime.MaxValue);
		}

		private bool CacheReplace(string key, object value, DateTime expiresAt)
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
				if (cacheEntry.ExpiresAt < DateTime.Now)
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
			return UpdateCounter(key, 1);
		}

		public long Decrement(string key, uint amount)
		{
			return UpdateCounter(key, -1);
		}

		public bool Add<T>(string key, T value)
		{
			return CacheAdd(key, value);
		}

		public bool Set<T>(string key, T value)
		{
			return CacheSet(key, value);
		}

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
	}
}