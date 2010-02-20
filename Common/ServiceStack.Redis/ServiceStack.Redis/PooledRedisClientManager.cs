using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common.Web;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Provides thread-safe pooling of redis clients.
	/// Allows the configuration of different ReadWrite and ReadOnly hosts
	/// </summary>
	public class PooledRedisClientManager
		: IRedisClientsManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(PooledRedisClientManager));

		protected const int PoolSizeMultiplier = 4;

		private List<EndPoint> ReadWriteHosts { get; set; }
		private List<EndPoint> ReadOnlyHosts { get; set; }

		private RedisClient[] writeClients = new RedisClient[0];
		protected int writePoolIndex;

		private RedisClient[] readClients = new RedisClient[0];
		protected int readPoolIndex;

		protected RedisClientManagerConfig Config { get; set; }

		public IRedisClientFactory RedisClientFactory { get; set; }

		public int Db { get; private set; }

		public PooledRedisClientManager() : this(RedisNativeClient.DefaultHost) { }

		public PooledRedisClientManager(params string[] readWriteHosts)
			: this(readWriteHosts, readWriteHosts)
		{
		}

		public PooledRedisClientManager(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts)
			: this(readWriteHosts, readOnlyHosts, null)
		{
		}

		/// <summary>
		/// Hosts can be an IP Address or Hostname in the format: host[:port]
		/// e.g. 127.0.0.1:6379
		/// default is: localhost:6379
		/// </summary>
		/// <param name="readWriteHosts">The write hosts.</param>
		/// <param name="readOnlyHosts">The read hosts.</param>
		/// <param name="config">The config.</param>
		public PooledRedisClientManager(
			IEnumerable<string> readWriteHosts,
			IEnumerable<string> readOnlyHosts,
			RedisClientManagerConfig config)
			: this(readWriteHosts, readOnlyHosts, config, RedisNativeClient.DefaultDb)
		{
		}

		public PooledRedisClientManager(
			IEnumerable<string> readWriteHosts,
			IEnumerable<string> readOnlyHosts,
			int initalDb)
			: this(readWriteHosts, readOnlyHosts, null, initalDb)
		{
		}

		public PooledRedisClientManager(
			IEnumerable<string> readWriteHosts,
			IEnumerable<string> readOnlyHosts,
			RedisClientManagerConfig config,
			int initalDb)
		{
			this.Db = config != null
				? config.DefaultDb.GetValueOrDefault(initalDb)
				: initalDb;

			ReadWriteHosts = readWriteHosts.ToIpEndPoints();
			ReadOnlyHosts = readOnlyHosts.ToIpEndPoints();

			this.RedisClientFactory = Redis.RedisClientFactory.Instance;

			this.Config = config ?? new RedisClientManagerConfig {
				MaxWritePoolSize = ReadWriteHosts.Count * PoolSizeMultiplier,
				MaxReadPoolSize = ReadOnlyHosts.Count * PoolSizeMultiplier,
			};

			if (this.Config.AutoStart)
			{
				this.OnStart();
			}
		}

		protected virtual void OnStart()
		{
			this.Start();
		}

		/// <summary>
		/// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
		/// </summary>
		/// <returns></returns>
		public IRedisClient GetClient()
		{
			lock (writeClients)
			{
				AssertValidReadWritePool();

				RedisClient inActiveClient;
				while ((inActiveClient = GetInActiveWriteClient()) == null)
				{
					Monitor.Wait(writeClients);
				}

				writePoolIndex++;
				inActiveClient.Active = true;

				//Reset database to default if changed
				if (inActiveClient.Db != Db)
				{
					inActiveClient.Db = Db;
				}

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
			lock (readClients)
			{
				AssertValidReadOnlyPool();

				RedisClient inActiveClient;
				while ((inActiveClient = GetInActiveReadClient()) == null)
				{
					Monitor.Wait(readClients);
				}

				readPoolIndex++;
				inActiveClient.Active = true;

				//Reset database to default if changed
				if (inActiveClient.Db != Db)
				{
					inActiveClient.Db = Db;
				}

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

		public void Start()
		{
			if (writeClients.Length > 0 || readClients.Length > 0)
				throw new InvalidOperationException("Pool has already been started");

			writeClients = new RedisClient[Config.MaxWritePoolSize];
			writePoolIndex = 0;

			readClients = new RedisClient[Config.MaxReadPoolSize];
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
			if (redisClient == null) return;
			try
			{
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