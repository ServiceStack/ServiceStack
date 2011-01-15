using System;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public abstract class AsyncRestServiceClientTests
	{
		private const string ListeningOn = "http://localhost:82/";

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

		protected abstract IRestClientAsync CreateServiceClient();

		private static void FailOnAsyncError<T>(T response, Exception ex)
		{
			Assert.Fail(ex.Message);
		}

		[Test]
		public void Can_call_GetAsync_on_RestClientAsync()
		{
			var asyncClient = CreateServiceClient();

			GetFactorialResponse response = null;
			asyncClient.GetAsync<GetFactorialResponse>("factorial/3", r => response = r, FailOnAsyncError);

			Thread.Sleep(1000);

			Assert.That(response, Is.Not.Null, "No response received");
			Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(3)));
		}

		[TestFixture]
		public class JsonAsyncRestServiceClientTests : AsyncRestServiceClientTests
		{
			protected override IRestClientAsync CreateServiceClient()
			{
				return new JsonRestClientAsync(ListeningOn);
			}
		}

		[TestFixture]
		public class JsvAsyncRestServiceClientTests : AsyncRestServiceClientTests
		{
			protected override IRestClientAsync CreateServiceClient()
			{
				return new JsvRestClientAsync(ListeningOn);
			}
		}

		[TestFixture]
		public class XmlAsyncRestServiceClientTests : AsyncRestServiceClientTests
		{
			protected override IRestClientAsync CreateServiceClient()
			{
				return new XmlRestClientAsync(ListeningOn);
			}
		}
	}
}