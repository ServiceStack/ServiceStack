using System;
using System.Collections.Generic;
using System.Threading;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
	[TestFixture]
	public class MultiThreadedRedisClientIntegrationTests
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

		[Test]
		public void Can_support_64_threads_using_the_client_simultaneously()
		{
			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

			var clientAsyncResults = new List<IAsyncResult>();
			using (var redisClient = new RedisClient(masterHosts[0]))
			{
				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					var action = (Action)(() => UseClientAsync(redisClient, clientNo));
					clientAsyncResults.Add(action.BeginInvoke(null, null));
				}
			}

			WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());
		}

		[Test]
		public void Can_support_64_threads_using_the_client_sequentially()
		{
			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

			using (var redisClient = new RedisClient(masterHosts[0]))
			{
				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					UseClient(redisClient, clientNo);
				}
			}
		}

		private void UseClientAsync(RedisClient client, int clientNo)
		{
			lock (this)
			{
				UseClient(client, clientNo);
			}
		}

		private static void UseClient(RedisClient client, int clientNo)
		{
			var host = "";

			try
			{
				host = client.Host;

				Console.WriteLine("Client '{0}' is using '{1}'", clientNo, client.Host);

				var testClientKey = "test:" + host + ":" + clientNo;
				client.SetString(testClientKey, testData);
				var result = client.GetString(testClientKey) ?? "";

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