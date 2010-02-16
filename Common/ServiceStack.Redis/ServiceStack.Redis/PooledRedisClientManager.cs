using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Provides thread-safe pooling of redis clients.
	/// Allows the configuration of different ReadWrite and ReadOnly hosts
	/// </summary>
	public class PooledRedisClientManager : IRedisClientsManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(PooledRedisClientManager));

		private List<EndPoint> ReadWriteHosts { get; set; }
		private List<EndPoint> ReadOnlyHosts { get; set; }

		public int MaxWritePoolSize { get; set; }
		private RedisClient[] writeClients = new RedisClient[0];
		protected int writePoolIndex;

		public int MaxReadPoolSize { get; set; }
		private RedisClient[] readClients = new RedisClient[0];
		protected int readPoolIndex;

		public IRedisClientFactory RedisClientFactory { get; set; }

		public PooledRedisClientManager() : this(RedisNativeClient.DefaultHost) { }

		public PooledRedisClientManager(params string[] readWriteHosts)
			: this(readWriteHosts, readWriteHosts)
		{
		}

		/// <summary>
		/// Hosts can be an IP Address or Hostname in the format: host[:port]
		/// e.g. 127.0.0.1:6379
		/// default is: localhost:6379
		/// </summary>
		/// <param name="writeHosts">The write hosts.</param>
		/// <param name="readHosts">The read hosts.</param>
		public PooledRedisClientManager(IEnumerable<string> writeHosts, IEnumerable<string> readHosts)
		{
			ReadWriteHosts = ConvertToIpEndPoints(writeHosts);
			ReadOnlyHosts = ConvertToIpEndPoints(readHosts);

			this.MaxWritePoolSize = ReadWriteHosts.Count;
			this.MaxReadPoolSize = ReadOnlyHosts.Count;

			this.RedisClientFactory = Redis.RedisClientFactory.Instance;
		}

		internal class EndPoint
		{
			internal string Host { get; private set; }
			internal int Port { get; private set; }

			public EndPoint(string host, int port)
			{
				Host = host;
				Port = port;
			}
		}

		/// <summary>
		/// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
		/// </summary>
		/// <returns></returns>
		public IRedisClient GetClient()
		{
			AssertValidReadWritePool();

			lock (writeClients)
			{
				RedisClient inActiveClient;
				while ((inActiveClient = GetInActiveWriteClient()) == null)
				{
					Monitor.Wait(writeClients);
				}

				writePoolIndex++;
				inActiveClient.Active = true;
				return inActiveClient;
			}
		}

		private RedisClient GetInActiveWriteClient()
		{
			for (var i=0; i < writeClients.Length; i++)
			{
				var nextIndex = (writePoolIndex + i) % writeClients.Length;

				//Initialize if not exists
				if (writeClients[nextIndex] == null)
				{
					var nextHost = ReadWriteHosts[nextIndex % ReadWriteHosts.Count];
					
					var client = RedisClientFactory.CreateRedisClient(
						nextHost.Host, nextHost.Port);

					client.ClientManager = this;

					writeClients[nextIndex] = client;

					return client;
				}

				//look for free one
				if (!writeClients[nextIndex].Active)
				{
					return writeClients[nextIndex];
				}
			}
			return null;
		}

		/// <summary>
		/// Returns a ReadOnly client using the hosts defined in ReadOnlyHosts.
		/// </summary>
		/// <returns></returns>
		public virtual IRedisClient GetReadOnlyClient()
		{
			AssertValidReadOnlyPool();

			lock (readClients)
			{
				RedisClient inActiveClient;
				while ((inActiveClient = GetInActiveReadClient()) == null)
				{
					Monitor.Wait(readClients);
				}

				readPoolIndex++;
				inActiveClient.Active = true;
				return inActiveClient;
			}
		}

		private RedisClient GetInActiveReadClient()
		{
			for (var i=0; i < readClients.Length; i++)
			{
				var nextIndex = (readPoolIndex + i) % readClients.Length;

				//Initialize if not exists
				if (readClients[nextIndex] == null)
				{
					var nextHost = ReadOnlyHosts[nextIndex % ReadOnlyHosts.Count];
					var client = RedisClientFactory.CreateRedisClient(
						nextHost.Host, nextHost.Port);
					
					client.ClientManager = this;

					readClients[nextIndex] = client;
					
					return client;
				}

				//look for free one
				if (!readClients[nextIndex].Active)
				{
					return readClients[nextIndex];
				}
			}
			return null;
		}

		public void DisposeClient(RedisNativeClient client)
		{
			lock (readClients)
			{
				for (var i = 0; i < readClients.Length; i++)
				{
					var readClient = readClients[i];
					if (client != readClient) continue;
					client.Active = false;
					Monitor.PulseAll(readClients);
					return;
				}
			}

			lock (writeClients)
			{
				for (var i = 0; i < writeClients.Length; i++)
				{
					var writeClient = writeClients[i];
					if (client != writeClient) continue;
					client.Active = false;
					Monitor.PulseAll(writeClients);
					return;
				}
			}

			throw new NotSupportedException("Cannot add unknown client back to the pool");
		}

		private static List<EndPoint> ConvertToIpEndPoints(IEnumerable<string> hosts)
		{
			if (hosts == null) return new List<EndPoint>();

			const int hostOrIpAddressIndex = 0;
			const int portIndex = 1;

			var ipEndpoints = new List<EndPoint>();
			foreach (var host in hosts)
			{
				var hostParts = host.Split(':');
				if (hostParts.Length == 0)
					throw new ArgumentException("'{0}' is not a valid Host or IP Address: e.g. '127.0.0.0[:11211]'");

				var port = (hostParts.Length == 1)
					? RedisNativeClient.DefaultPort : int.Parse(hostParts[portIndex]);

				var endpoint = new EndPoint(hostParts[hostOrIpAddressIndex], port);
				ipEndpoints.Add(endpoint);
			}
			return ipEndpoints;
		}

		public void Start()
		{
			writeClients = new RedisClient[MaxWritePoolSize];
			writePoolIndex = 0;

			readClients = new RedisClient[MaxReadPoolSize];
			readPoolIndex = 0;
		}

		private void AssertValidReadWritePool()
		{
			if (writeClients.Length < 1)
				throw new InvalidOperationException("Need a minimum read-write pool size of 1, then call Start()");
		}

		private void AssertValidReadOnlyPool()
		{
			if (readClients.Length < 1)
				throw new InvalidOperationException("Need a minimum read pool size of 1, then call Start()");
		}

		~PooledRedisClientManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// get rid of managed resources
			}

			// get rid of unmanaged resources
			for (var i = 0; i < writeClients.Length; i++)
			{
				Dispose(writeClients[i]);
			}
			for (var i = 0; i < readClients.Length; i++)
			{
				Dispose(readClients[i]);
			}
		}

		protected void Dispose(RedisClient redisClient)
		{
			try
			{
				if (redisClient == null) return;
				
				redisClient.DisposeConnection();
			}
			catch (Exception ex)
			{
				Log.Error(string.Format(
					"Error when trying to dispose of RedisClient to host {0}:{1}",
					redisClient.Host, redisClient.Port), ex);
			}
		}
	}
}