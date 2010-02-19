using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
	[TestFixture]
	public class MultiThreadedPoolIntegrationTests
	{
		readonly string [] masterHosts = new[] { "chi-dev-mem1.ddnglobal.local" };
		readonly string [] slaveHosts = new[] { "chi-dev-mem1.ddnglobal.local", "chi-dev-mem2.ddnglobal.local" };

		public PooledRedisClientManager CreateAndStartManager(
			string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new PooledRedisClientManager(readWriteHosts, readOnlyHosts,
				new RedisClientManagerConfig {
					MaxWritePoolSize = readWriteHosts.Length,
					MaxReadPoolSize = readOnlyHosts.Length,
					AutoStart = true,
				});
		}

		[Test]
		public void Can_support_64_threads_using_the_client_simultaneously()
		{
			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64
			var clientUsageMap = new Dictionary<string, int>();

			var clientAsyncResults = new List<IAsyncResult>();
			using (var manager = CreateAndStartManager(masterHosts, slaveHosts))
			{
				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					var action = (Action)(() => UseClient(manager, clientNo, clientUsageMap));
					clientAsyncResults.Add(action.BeginInvoke(null, null));
				}
			}

			WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

			Console.WriteLine(TypeSerializer.SerializeToString(clientUsageMap));

			var hostCount = 0;
			foreach (var entry in clientUsageMap)
			{
				Assert.That(entry.Value, Is.GreaterThanOrEqualTo(5), "Host has unproportianate distrobution: " + entry.Value);
				Assert.That(entry.Value, Is.LessThanOrEqualTo(60), "Host has unproportianate distrobution: " + entry.Value);
				hostCount += entry.Value;
			}

			Assert.That(hostCount, Is.EqualTo(noOfConcurrentClients), "Invalid no of clients used");
		}

		private static void UseClient(IRedisClientsManager manager, int clientNo, Dictionary<string, int> hostCountMap)
		{
			using (var client = manager.GetReadOnlyClient())
			{
				lock (hostCountMap)
				{
					int hostCount;
					if (!hostCountMap.TryGetValue(client.Host, out hostCount))
					{
						hostCount = 0;
					}

					hostCountMap[client.Host] = ++hostCount;
				}

				Console.WriteLine("Client '{0}' is using '{1}'", clientNo, client.Host);
			}
		}

	}
}