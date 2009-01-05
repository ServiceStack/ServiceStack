using System;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess.Providers
{
	public class MemoryCacheClient : ICacheClient
	{
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

		private bool CacheAdd(string key, object value, DateTime expiresAt)
		{
			if (!this.memory.ContainsKey(key))
			{
				var entry = new CacheEntry(value, expiresAt);
				this.memory.Add(key, entry);
				return true;
			}
			this.memory[key].Value = value;
			this.memory[key].ExpiresAt = expiresAt;
			return false;
		}

		private bool CacheSet(string key, object value)
		{
			return CacheSet(key, value, DateTime.MaxValue);
		}

		private bool CacheSet(string key, object value, DateTime expiresAt)
		{
			return CacheSet(key, value, expiresAt, null);
		}

		private bool CacheSet(string key, object value, DateTime expiresAt, long? checkLastModified)
		{
			if (!this.memory.ContainsKey(key)) return false;
			var cacheEntry = this.memory[key];
			
			if (checkLastModified.HasValue && cacheEntry.LastModifiedTicks != checkLastModified.Value) return false;
			
			cacheEntry.Value = value;
			cacheEntry.ExpiresAt = expiresAt;
			return true;
		}

		private bool CacheReplace(string key, object value)
		{
			return CacheReplace(key, value, DateTime.MaxValue);
		}

		private bool CacheReplace(string key, object value, DateTime expiresAt)
		{
			return !CacheAdd(key, value, expiresAt);
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

		public IDictionary<string, object> Get(IEnumerable<string> keys)
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

		public IDictionary<string, object> Get(IEnumerable<string> keys, out IDictionary<string, ulong> lostModifiedValues)
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