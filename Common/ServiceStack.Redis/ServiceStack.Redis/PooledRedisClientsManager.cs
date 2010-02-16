using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Allows pooling of redis clients and the ability
	/// </summary>
	public class PooledRedisClientsManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(PooledRedisClientsManager));

		private List<IPEndPoint> WriteOnlyHosts { get; set; }
		private List<IPEndPoint> ReadOnlyHosts { get; set; }

		public int MaxWritePoolSize { get; set; }
		private RedisClient[] writeClients;
		protected int WritePoolIndex;

		public int MaxReadPoolSize { get; set; }
		private RedisClient[] readClients;
		protected int ReadPoolIndex;

		public virtual RedisClient CreateRedisClient(IPEndPoint hostEndpoint)
		{
			return new RedisClient(hostEndpoint.Address.ToString(), hostEndpoint.Port);
		}

		public IRedisClient GetWriteClient()
		{
			lock (writeClients)
			{
				RedisClient inActiveClient;
				while ((inActiveClient = GetInActiveWriteClient()) == null)
				{
					Monitor.Wait(writeClients);
				}

				WritePoolIndex++;
				inActiveClient.Active = true;
				return inActiveClient;
			}
		}

		private RedisClient GetInActiveWriteClient()
		{
			for (var i=0; i < writeClients.Length; i++)
			{
				var nextIndex = (WritePoolIndex + i) % writeClients.Length;

				//Initialize if not exists
				if (writeClients[nextIndex] == null)
				{
					writeClients[nextIndex] = CreateRedisClient(WriteOnlyHosts[nextIndex]);
					return writeClients[nextIndex];
				}

				//look for free one
				if (!writeClients[nextIndex].Active)
				{
					return writeClients[nextIndex];
				}
			}
			return null;
		}

		public virtual IRedisClient GetReadClient()
		{
			lock (readClients)
			{
				RedisClient inActiveClient;
				while ((inActiveClient = GetInActiveReadClient()) == null)
				{
					Monitor.Wait(readClients);
				}

				ReadPoolIndex++;
				inActiveClient.Active = true;
				return inActiveClient;
			}
		}

		private RedisClient GetInActiveReadClient()
		{
			for (var i=0; i < readClients.Length; i++)
			{
				var nextIndex = (ReadPoolIndex + i) % readClients.Length;

				//Initialize if not exists
				if (readClients[nextIndex] == null)
				{
					readClients[nextIndex] = CreateRedisClient(ReadOnlyHosts[nextIndex]);
					return readClients[nextIndex];
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
			foreach (var readClient in readClients)
			{
				if (client != readClient) continue;
				client.Active = false;
				Monitor.PulseAll(readClients);
				return;
			}

			foreach (var writeClient in writeClients)
			{
				if (client != writeClient) continue;
				client.Active = false;
				Monitor.PulseAll(writeClients);
				return;
			}

			throw new NotSupportedException("Cannot add unknown client back to the pool");
		}

		public PooledRedisClientsManager() : this(RedisNativeClient.DefaultHost) { }

		public PooledRedisClientsManager(params string[] readWriteHosts)
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
		public PooledRedisClientsManager(IEnumerable<string> writeHosts, IEnumerable<string> readHosts)
		{
			WriteOnlyHosts = ConvertToIpEndPoints(writeHosts);
			ReadOnlyHosts = ConvertToIpEndPoints(readHosts);

			this.MaxWritePoolSize = WriteOnlyHosts.Count;
			this.MaxReadPoolSize = ReadOnlyHosts.Count;
		}

		private static List<IPEndPoint> ConvertToIpEndPoints(IEnumerable<string> hosts)
		{
			const int hostOrIpAddressIndex = 0;
			const int portIndex = 1;

			var ipEndpoints = new List<IPEndPoint>();
			foreach (var host in hosts)
			{
				var hostParts = host.Split(':');
				if (hostParts.Length == 0)
					throw new ArgumentException("'{0}' is not a valid Host or IP Address: e.g. '127.0.0.0[:11211]'");

				var port = (hostParts.Length == 1)
					? RedisNativeClient.DefaultPort : int.Parse(hostParts[portIndex]);

				var hostAddresses = Dns.GetHostAddresses(hostParts[hostOrIpAddressIndex]);
				foreach (var ipAddress in hostAddresses)
				{
					var endpoint = new IPEndPoint(ipAddress, port);
					ipEndpoints.Add(endpoint);
				}
			}
			return ipEndpoints;
		}

		public void Start()
		{
			if (MaxWritePoolSize < 1)
				throw new ArgumentException("Need a minimum write pool size of 1");
			if (MaxReadPoolSize < 1)
				throw new ArgumentException("Need a minimum read pool size of 1");

			writeClients = new RedisClient[MaxWritePoolSize];
			WritePoolIndex = 0;

			readClients = new RedisClient[MaxReadPoolSize];
			ReadPoolIndex = 0;
		}

		~PooledRedisClientsManager()
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
			foreach (var writeClient in writeClients)
			{
				Dispose(writeClient);
			}
			foreach (var readClient in readClients)
			{
				Dispose(readClient);
			}
		}

		protected void Dispose(RedisClient redisClient)
		{
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