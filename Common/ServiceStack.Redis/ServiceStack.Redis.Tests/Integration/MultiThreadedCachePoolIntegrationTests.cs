using System;
using System.Collections.Generic;
using System.Threading;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
	[TestFixture]
	public class MultiThreadedCachePoolIntegrationTests
	{
		readonly string [] masterHosts = new[] { "chi-dev-mem1.ddnglobal.local" };
		readonly string [] slaveHosts = new[] { "chi-dev-mem1.ddnglobal.local", "chi-dev-mem2.ddnglobal.local" };

		private static string testData;

		[TestFixtureSetUp]
		public void onBeforeTestFixture()
		{
			NorthwindData.LoadData(false);

			testData = TypeSerializer.SerializeToString(NorthwindData.Customers);
		}

		public PooledRedisClientCacheManager CreateAndStartManager(
			string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new PooledRedisClientCacheManager(readWriteHosts, readOnlyHosts);
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
		}

		private static void UseClient(PooledRedisClientCacheManager manager, int clientNo, Dictionary<string, int> hostCountMap)
		{
			var host = "";

			try
			{
				using (var client = manager.GetReadOnlyClient())
				{
					host = client.Host;

					Console.WriteLine("Client '{0}' is using '{1}'", clientNo, client.Host);
				}

				var testClientKey = "test:" + host + ":" + clientNo;
				manager.Set(testClientKey, testData);
				var result = manager.Get<string>(testClientKey) ?? "";

				Console.WriteLine("\t{0} => {1} len {2} {3} len", testClientKey,
					testData.Length, testData.Length == result.Length ? "==" : "!=", result.Length);

			}
			catch (NullReferenceException ex)
			{
				Console.WriteLine("NullReferenceException StackTrace: \n" + ex.StackTrace);
			}
			catch (Exception ex)
			{
				Console.WriteLine("\t[ERROR@{0}]: {1} => {2}", 
					host, ex.GetType().Name, ex.Message);
			}
		}

	}
}