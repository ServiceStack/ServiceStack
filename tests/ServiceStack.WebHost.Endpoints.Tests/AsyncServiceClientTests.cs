using System;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Clients;
using ServiceStack.Clients;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public abstract class AsyncServiceClientTests
	{
		protected const string ListeningOn = "http://localhost:82/";

		ExampleAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
		}

		protected abstract IServiceClient CreateServiceClient();

		private static void FailOnAsyncError<T>(T response, Exception ex)
		{
			Assert.Fail(ex.Message);
		}

		[Test]
		public void Can_call_SendAsync_on_ServiceClient()
		{
			var jsonClient = new JsonServiceClient(ListeningOn);

			var request = new GetFactorial { ForNumber = 3 };
			GetFactorialResponse response = null;
			jsonClient.SendAsync<GetFactorialResponse>(request, r => response = r, FailOnAsyncError);

			Thread.Sleep(1000);

			Assert.That(response, Is.Not.Null, "No response received");
			Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
		}

		[TestFixture]
		public class JsonAsyncServiceClientTests : AsyncServiceClientTests
		{
			protected override IServiceClient CreateServiceClient()
			{
				return new JsonServiceClient(ListeningOn);
			}
		}

		[TestFixture]
		public class JsvAsyncServiceClientTests : AsyncServiceClientTests
		{
			protected override IServiceClient CreateServiceClient()
			{
				return new JsvServiceClient(ListeningOn);
			}
		}

		[TestFixture]
		public class XmlAsyncServiceClientTests : AsyncServiceClientTests
		{
			protected override IServiceClient CreateServiceClient()
			{
				return new XmlServiceClient(ListeningOn);
			}
		}
	}


}