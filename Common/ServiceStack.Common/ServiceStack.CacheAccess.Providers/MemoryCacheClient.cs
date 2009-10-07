using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.CacheAccess.Providers
{
	public class MemoryCacheClient : ICacheClient
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (MemoryCacheClient));

		private Dictionary<string, CacheEntry> memory;
		private Dictionary<string, int> counters;

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
			this.memory = new Dictionary<string, CacheEntry>();
			this.counters = new Dictionary<string, int>();
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
			CacheEntry entry;
			if (this.memory.TryGetValue(key, out entry)) return false;

			entry = new CacheEntry(value, expiresAt);
			this.memory.Add(key, entry);

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
			if (!this.memory.TryGetValue(key, out entry))
			{
				entry = new CacheEntry(value, expiresAt);
				this.memory.Add(key, entry);
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
			this.memory = new Dictionary<string, CacheEntry>();
			this.counters = new Dictionary<string, int>();
		}

		public bool Remove(string key)
		{
			if (this.memory.ContainsKey(key))
			{
				this.memory.Remove(key);
				return true;
			}
			return false;
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
			if (this.memory.ContainsKey(key))
			{
				var cacheEntry = this.memory[key];
				if (cacheEntry.ExpiresAt < DateTime.Now)
				{
					this.memory.Remove(key);
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

		public T Get<T>(string key, out ulong ucas)
		{
			IDictionary<string, ulong> casValues;
			var results = GetAll(new[] { key }, out casValues);

			object result;
			if (results.TryGetValue(key, out result))
			{
				ucas = casValues[key];
				return (T)result;
			}

			ucas = default(ulong);
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

		public bool Add(string key, object value)
		{
			return CacheAdd(key, value);
		}

		public bool Set(string key, object value)
		{
			return CacheSet(key, value);
		}

		public bool Replace(string key, object value)
		{
			return CacheReplace(key, value);
		}

		public bool Add(string key, object value, DateTime expiresAt)
		{
			return CacheAdd(key, value, expiresAt);
		}

		public bool Set(string key, object value, DateTime expiresAt)
		{
			return CacheSet(key, value, expiresAt);
		}

		public bool Replace(string key, object value, DateTime expiresAt)
		{
			return CacheReplace(key, value, expiresAt);
		}

		public bool CheckAndSet(string key, object value, ulong checkLastModified)
		{
			return CacheSet(key, value);
		}

		public bool CheckAndSet(string key, object value, ulong checkLastModified, DateTime expiresAt)
		{
			return CacheSet(key, value, expiresAt, (long)checkLastModified);
		}

		public void FlushAll()
		{
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			var valueMap = new Dictionary<string, T>();
			foreach (var key in keys)
			{
				var value = Get<T>(key);
				if (!Equals(value, default(T)))
				{
					valueMap[key] = value;
				}
			}
			return valueMap;
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys)
		{
			var valueMap = new Dictionary<string, object>();
			foreach (var key in keys)
			{
				var value = Get(key);
				if (value != null)
				{
					valueMap[key] = value;
				}
			}
			return valueMap;
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys, out IDictionary<string, ulong> lostModifiedValues)
		{
			var valueMap = new Dictionary<string, object>();
			lostModifiedValues = new Dictionary<string, ulong>();
			foreach (var key in keys)
			{
				long lostModifiedValue;
				var value = Get(key, out lostModifiedValue);
				if (value != null)
				{
					valueMap[key] = value;
					lostModifiedValues[key] = (ulong)lostModifiedValue;
				}
			}
			return valueMap;
		}
	}
}