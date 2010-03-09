using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Enyim.Caching;
using ServiceStack.Logging;
using InnerClient = Enyim.Caching;

namespace ServiceStack.CacheAccess.Memcached
{
	/// <summary>
	/// A memcached implementation of the ServiceStack ICacheClient interface.
	/// Good practice not to have dependencies on implementations in your business logic.
	/// 
	/// Basically delegates all calls to Enyim.Caching.MemcachedClient with added diagnostics and logging.
	/// </summary>
	public class MemcachedClientCache 
		: AdapterBase, ICacheClient, IMemcachedClient
	{
		protected override ILog Log { get { return LogManager.GetLogger(GetType()); } }

		private InnerClient.MemcachedClient client;

		public MemcachedClientCache(IEnumerable<string> hosts)
		{
			const int defaultPort = 11211;
			const int ipAddressIndex = 0;
			const int portIndex = 1;

			this.client = new MemcachedClient();
			var ipEndpoints = new List<IPEndPoint>();
			foreach (var host in hosts)
			{
				var hostParts = host.Split(':');
				if (hostParts.Length == 0)
					throw new ArgumentException("'{0}' is not a valid host IP Address: e.g. '127.0.0.0[:11211]'");

				var port = (hostParts.Length == 1) ? defaultPort : int.Parse(hostParts[portIndex]);

				var hostAddresses = Dns.GetHostAddresses(hostParts[ipAddressIndex]);
				foreach (var ipAddress in hostAddresses)
				{
					var endpoint = new IPEndPoint(ipAddress, port);
					ipEndpoints.Add(endpoint);
				}
			}
			LoadClient(ipEndpoints);
		}

		public MemcachedClientCache(IEnumerable<IPEndPoint> ipEndpoints)
		{
			LoadClient(ipEndpoints);
		}

		private void LoadClient(IEnumerable<IPEndPoint> ipEndpoints)
		{
			var config = new InnerClient.Configuration.MemcachedClientConfiguration();
			foreach (var ipEndpoint in ipEndpoints)
			{
				config.Servers.Add(ipEndpoint);
			}

			config.SocketPool.MinPoolSize = 10;
			config.SocketPool.MaxPoolSize = 100;
			config.SocketPool.ConnectionTimeout = new TimeSpan(0, 0, 10);
			config.SocketPool.DeadTimeout = new TimeSpan(0, 2, 0);

			this.client = new InnerClient.MemcachedClient(config);
		}

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

		public object Get(string key, out ulong ucas)
		{
			IDictionary<string, ulong> casValues;
			var results = GetAll(new[] { key }, out casValues);

			object result;
			if (results.TryGetValue(key, out result))
			{
				ucas = casValues[key];
				return result;
			}

			ucas = default(ulong);
			return null;
		}
		
		public string GetText(string key)
		{
			return Execute(() => Encoding.UTF8.GetString((byte[])client.Get(key)));
		}

		public byte[] GetBytes(string key)
		{
			return Execute(() => (byte[])client.Get(key));
		}

		public T Get<T>(string key)
		{
			return Execute(() => client.Get<T>(key));
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

		public long Increment(string key, uint amount)
		{
			return Execute(() => client.Increment(key, amount));
		}

		public long Decrement(string key, uint amount)
		{
			return Execute(() => client.Decrement(key, amount));
		}

		public bool Add<T>(string key, T value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, value));
		}

		public bool Set<T>(string key, T value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, value));
		}

		public bool Replace<T>(string key, T value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, value));
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, value, expiresAt));
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, value, expiresAt));
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, value, expiresAt));
		}

		public bool Add<T>(string key, T value, TimeSpan expiresIn)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, value, expiresIn));
		}

		public bool Set<T>(string key, T value, TimeSpan expiresIn)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, value, expiresIn));
		}

		public bool Replace<T>(string key, T value, TimeSpan expiresIn)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, value, expiresIn));
		}

		public bool Set(string key, byte[] value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, value));
		}

		public bool Add(string key, string value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, Encoding.UTF8.GetBytes(value)));
		}

		public bool Set(string key, string value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, Encoding.UTF8.GetBytes(value)));
		}

		public bool Replace(string key, string value)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, Encoding.UTF8.GetBytes(value)));
		}

		public bool Add(string key, string value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Add, key, Encoding.UTF8.GetBytes(value), expiresAt));
		}

		public bool Set(string key, string value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Set, key, Encoding.UTF8.GetBytes(value), expiresAt));
		}

		public bool Replace(string key, string value, DateTime expiresAt)
		{
			return Execute(() => client.Store(InnerClient.Memcached.StoreMode.Replace, key, Encoding.UTF8.GetBytes(value), expiresAt));
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

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			var results = new Dictionary<string, T>();
			foreach (var key in keys)
			{
				var result = this.Get<T>(key);
				results[key] = result;
			}

			return results;
		}

		public void SetAll<T>(IDictionary<string, T> values)
		{
			foreach (var entry in values)
			{
				Set(entry.Key, entry.Value);
			}
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys)
		{
			return Execute(() => client.Get(keys));
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys, out IDictionary<string, ulong> casValues)
		{
			//Can't call methods with 'out' params in anonymous method blocks
			//Calling client directly instead - Add try{} if warranted.
			return client.Get(keys, out casValues);
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
					Log.Error(string.Format("Error trying to remove {0} from memcached", key), ex);
				}
			}
		}

	}
}