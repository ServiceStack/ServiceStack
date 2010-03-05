using System;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests.Integration
{
	[TestFixture]
	public class MultiThreadedCacheClientManagerIntegrationTests
		: IntegrationTestBase
	{
		private static string testData;

		[TestFixtureSetUp]
		public void onBeforeTestFixture()
		{
			NorthwindData.LoadData(false);

			testData = TypeSerializer.SerializeToString(NorthwindData.Customers);
		}

		[Test]
		public void Pool_can_support_64_threads_using_the_client_simultaneously()
		{
			RunSimultaneously(CreateAndStartPoolManager, UseClient);
		}

		[Test]
		public void Basic_can_support_64_threads_using_the_client_simultaneously()
		{
			RunSimultaneously(CreateAndStartBasicCacheManager, UseClient);
		}

		private static void UseClient(IRedisClientsManager manager, int clientNo)
		{
			var cacheManager = (IRedisClientCacheManager)manager;

			var host = "";

			try
			{
				using (var client = cacheManager.GetReadOnlyCacheClient())
				{
					host = ((IRedisClient)client).Host;
					Log("Client '{0}' is using '{1}'", clientNo, host);

					var testClientKey = "test:" + host + ":" + clientNo;
					client.Set(testClientKey, testData);
					var result = client.Get<string>(testClientKey) ?? "";

					Log("\t{0} => {1} len {2} {3} len", testClientKey,
						testData.Length, testData.Length == result.Length ? "==" : "!=", result.Length);
				}
			}
			catch (NullReferenceException ex)
			{
				Log("NullReferenceException StackTrace: \n" + ex.StackTrace);
			}
			catch (Exception ex)
			{
				Log("\t[ERROR@{0}]: {1} => {2}",
					host, ex.GetType().Name, ex.Message);
			}
		}

	}
}