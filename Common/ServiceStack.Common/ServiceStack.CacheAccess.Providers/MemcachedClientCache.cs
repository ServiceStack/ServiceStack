using System;
using System.Collections.Generic;
using ServiceStack.Common.Support;
using ServiceStack.Logging;
using InnerClient=Enyim.Caching;

namespace ServiceStack.CacheAccess.Providers
{
	/// <summary>
	/// A memcached implementation of the ServiceStack ICacheClient interface.
	/// Good practice not to have dependencies on implementations in your business logic.
	/// 
	/// Basically delegates all calls to Enyim.Caching.MemcachedClient with added diagnostics and logging.
	/// </summary>
	public class MemcachedClientCache : AdapterBase, ICacheClient
	{
		private readonly InnerClient.MemcachedClient client;
		protected override ILog Log { get { return LogManager.GetLogger(GetType()); } }

		public MemcachedClientCache(InnerClient.MemcachedClient client)
		{
			if (client == null)
			{
				throw new ArgumentNullException("client");
			}
			this.client = client;
		}

		public void Dispose()
		{
			Execute(() => client.Dispose());
		}

		public bool Remove(string key)
		{
			return Execute(() => client.Remove(key));
		}

		public object Get(string key)
		{
			return Execute(() => client.Get(key));
		}

		public T Get<T>(string key)
		{
			return Execute(() => client.Get<T>(key));
		}

		public long Increment(string key, uint amount)
		{
			return Execute(() => client.Increment(key, amount));
		}

		public long Decrement(string key, uint amount)
		{
			return Execute(() => client.Decrement(key, amount));
		}

		public bool Add(string key, object value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, value));
		}

		public bool Set(string key, object value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, value));
		}

		public bool Replace(string key, object value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, value));
		}

		public bool Add(string key, object value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, value, expiresAt));
		}

		public bool Set(string key, object value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, value, expiresAt));
		}

		public bool Replace(string key, object value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, value, expiresAt));
		}

		public bool Append(string key, byte[] data)
		{
			return Execute(() => client.Append(key, data));
		}

		public bool Prepend(string key, byte[] data)
		{
			return Execute(() => client.Prepend(key, data));
		}

		public bool CheckAndSet(string key, object value, ulong cas)
		{
			return Execute(() => client.CheckAndSet(key, value, cas));
		}

		public bool CheckAndSet(string key, byte[] value, int offset, int length, ulong cas)
		{
			return Execute(() => client.CheckAndSet(key, value, offset, length, cas));
		}

		public bool CheckAndSet(string key, object value, ulong cas, TimeSpan validFor)
		{
			return Execute(() => client.CheckAndSet(key, value, cas, validFor));
		}

		public bool CheckAndSet(string key, object value, ulong cas, DateTime expiresAt)
		{
			return Execute(() => client.CheckAndSet(key, value, cas, expiresAt));
		}

		public bool CheckAndSet(string key, byte[] value, int offset, int length, ulong cas, TimeSpan validFor)
		{
			return Execute(() => client.CheckAndSet(key, value, offset, length, cas, validFor));
		}

		public bool CheckAndSet(string key, byte[] value, int offset, int length, ulong cas, DateTime expiresAt)
		{
			return Execute(() => client.CheckAndSet(key, value, offset, length, cas, expiresAt));
		}

		public void FlushAll()
		{
			Execute(() => client.FlushAll());
		}

		public IDictionary<string, object> Get(IEnumerable<string> keys)
		{
			return Execute(() => client.Get(keys));
		}

		public IDictionary<string, object> Get(IEnumerable<string> keys, out IDictionary<string, ulong> casValues)
		{
			//Can't call methods with 'out' params in anonymous method blocks
			//Calling client directly instead - Add try{} if warranted.
			return client.Get(keys, out casValues);
		}
	}
}