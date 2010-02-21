using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
	[TestFixture]
	public class RedisRegressionTestRun
	{
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
			var before = Stopwatch.GetTimestamp();

			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

			var clientAsyncResults = new List<IAsyncResult>();
			using (var manager = new PooledRedisClientManager(TestConfig.MasterHosts, TestConfig.SlaveHosts))
			{
				manager.GetClient().Run(x => x.FlushAll());

				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					var action = (Action)(() => UseClientAsync(manager, clientNo));
					clientAsyncResults.Add(action.BeginInvoke(null, null));
				}
			}

			WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

			Console.WriteLine("Completed in {0} ticks", (Stopwatch.GetTimestamp() - before));
		}

		[Test]
		public void Can_run_series_of_operations_sequentially()
		{
			var before = Stopwatch.GetTimestamp();

			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64

			using (var redisClient = new RedisClient(TestConfig.SingleHost))
			{
				redisClient.FlushAll();

				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					UseClient(redisClient, clientNo);
				}
			}

			Console.WriteLine("Completed in {0} ticks", (Stopwatch.GetTimestamp() - before));
		}

		private static void UseClientAsync(IRedisClientsManager manager, int clientNo)
		{
			using (var client = manager.GetReadOnlyClient())
			{
				UseClient(client, clientNo);
			}
		}

		private static void UseClient(IRedisClient client, int clientNo)
		{
			var host = "";

			try
			{
				host = client.Host;

				Console.WriteLine("Client '{0}' is using '{1}'", clientNo, client.Host);
				var differentDbs = new[] { 1, 0, 2 };

				foreach (var db in differentDbs)
				{
					client.Db = db;

					var testClientKey = "test:" + host + ":" + clientNo;
					client.SetString(testClientKey, testData);
					var result = client.GetString(testClientKey) ?? "";
					LogResult(db, testClientKey, result);

					var testClientSetKey = "test+set:" + host + ":" + clientNo;
					client.AddToSet(testClientSetKey, testData);
					var resultSet = client.GetAllFromSet(testClientSetKey);
					LogResult(db, testClientKey, resultSet.ToList().FirstOrDefault());

					var testClientListKey = "test+list:" + host + ":" + clientNo;
					client.AddToList(testClientListKey, testData);
					var resultList = client.GetAllFromList(testClientListKey);
					LogResult(db, testClientKey, resultList.FirstOrDefault());

				}
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

		private static void LogResult(int db, string testClientKey, string resultData)
		{
			if (resultData.IsNullOrEmpty())
			{
				Console.WriteLine("\tERROR@[{0}] NULL", db);
				return;
			}

			Console.WriteLine("\t[{0}] {1} => {2} len {3} {4} len",
			  db,
			  testClientKey,
			  testData.Length,
			  testData.Length == resultData.Length ? "==" : "!=", resultData.Length);
		}
	}

}