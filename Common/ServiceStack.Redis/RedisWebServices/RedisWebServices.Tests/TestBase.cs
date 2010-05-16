using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Redis;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.Tests
{
	[TestFixture]
	public class TestBase
	{
		protected const string TestKey = "testkey";
		protected const string TestValue = "Hello";
		protected static readonly List<string> StringValues = new List<string> { "A", "B", "C" };
		protected static List<int> IntValues = new List<int> { 1, 2, 3 };


		public static TestHost TestHost;
		public static TestConfig TestConfig;

		static TestBase()
		{
			TestHost = new TestHost();
			TestHost.Init();

			TestConfig = new TestConfig(new ConfigurationResourceManager());
		}

		[TestFixtureSetUp]
		public virtual void TestFixtureSetUp()
		{
			this.ServiceClient = !TestConfig.RunIntegrationTests
				? (IServiceClient)new TestServiceClient(TestHost)
				: new XmlServiceClient(TestConfig.IntegrationTestsBaseUrl);
		}

		[SetUp]
		public virtual void OnBeforeEachTest()
		{
			RedisExec(r => r.FlushAll());
		}

		protected IServiceClient ServiceClient { get; set; }

		protected void SendOneWay(object request)
		{
			this.ServiceClient.SendOneWay(request);
		}

		protected T Send<T>(object request, Func<T, ResponseStatus> getResponseStatusFn)
		{
			var response = this.ServiceClient.Send<T>(request);
			var responseStatus = getResponseStatusFn(response);

			if (responseStatus.ErrorCode != null)
			{
				throw new ServiceResponseException(responseStatus);
			}

			return response;
		}

		protected void RedisExec(Action<IRedisClient> redisFn)
		{
			using (var redisClient = TestHost.Container.Resolve<IRedisClientsManager>().GetClient())
			{
				redisFn(redisClient);
			}
		}

		protected T RedisExec<T>(Func<IRedisClient, T> redisFn)
		{
			using (var redisClient = TestHost.Container.Resolve<IRedisClientsManager>().GetClient())
			{
				return redisFn(redisClient);
			}
		}

		protected T RedisNativeExec<T>(Func<IRedisNativeClient, T> redisFn)
		{
			using (var redisClient = TestHost.Container.Resolve<IRedisClientsManager>().GetClient())
			{
				return redisFn((IRedisNativeClient)redisClient);
			}
		}

		protected void RedisNativeExec(Action<IRedisNativeClient> redisFn)
		{
			using (var redisClient = TestHost.Container.Resolve<IRedisClientsManager>().GetClient())
			{
				redisFn((IRedisNativeClient)redisClient);
			}
		}

	}
}