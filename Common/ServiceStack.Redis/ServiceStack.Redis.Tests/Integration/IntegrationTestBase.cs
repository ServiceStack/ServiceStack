using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
	public class IntegrationTestBase
	{
		protected IRedisClientsManager CreateAndStartPoolCacheManager(
			string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new PooledRedisClientCacheManager(readWriteHosts, readOnlyHosts);
		}

		protected IRedisClientsManager CreateAndStartPoolManager(
			string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new PooledRedisClientManager(readWriteHosts, readOnlyHosts);
		}

		protected IRedisClientsManager CreateAndStartBasicCacheManager(
			string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new BasicRedisClientCacheManager(readWriteHosts, readOnlyHosts);
		}

		protected IRedisClientsManager CreateAndStartBasicManager(
			string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new BasicRedisClientManager(readWriteHosts, readOnlyHosts);
		}

		[Conditional("DEBUG")]
		protected static void Log(string fmt, params object[] args)
		{
			Console.WriteLine(fmt, args);
		}

		protected void RunSimultaneously(
			Func<string[], string[], IRedisClientsManager> clientManagerFactory,
			Action<IRedisClientsManager, int> useClientFn)
		{
			var before = Stopwatch.GetTimestamp();

			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

			var clientAsyncResults = new List<IAsyncResult>();
			using (var manager = clientManagerFactory(TestConfig.MasterHosts, TestConfig.SlaveHosts))
			{
				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					var action = (Action)(() => useClientFn(manager, clientNo));
					clientAsyncResults.Add(action.BeginInvoke(null, null));
				}
			}

			WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

			Console.WriteLine("Time Taken: {0}", (Stopwatch.GetTimestamp() - before) / 1000);
		}

		protected static void CheckHostCountMap(Dictionary<string, int> hostCountMap)
		{
			Console.WriteLine(TypeSerializer.SerializeToString(hostCountMap));

			if (TestConfig.SlaveHosts.Length <= 1) return;

			var hostCount = 0;
			foreach (var entry in hostCountMap)
			{
				if (entry.Value < 5)
				{
					Console.WriteLine("ERROR: Host has unproportianate distrobution: " + entry.Value);
				}
				if (entry.Value > 60)
				{
					Console.WriteLine("ERROR: Host has unproportianate distrobution: " + entry.Value);
				}
				hostCount += entry.Value;
			}

			if (hostCount != 64)
			{
				Console.WriteLine("ERROR: Invalid no of clients used");
			}
		}

	}
}