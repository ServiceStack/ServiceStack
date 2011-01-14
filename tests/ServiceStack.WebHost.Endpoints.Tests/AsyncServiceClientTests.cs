using System;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class AsyncServiceClientTests
	{
		static AsyncServiceClientTests()
		{
			LogManager.LogFactory = new ConsoleLogFactory();
		}

		private const string ListeningOn = "http://localhost:82/";
		private const string BaseUri = ListeningOn + "ServiceStack/";

		private readonly GetFactorial requestDto = new GetFactorial { ForNumber = 3 };

		ExampleAppHost appHost;

		[SetUp]
		public void OnBeforeEachTest()
		{
			if (appHost != null)
			{
				appHost.Dispose();
				appHost = null;
			}
			appHost = new ExampleAppHost();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[Test]
		public void Can_call_using_JsonAsyncServiceClient()
		{
			var jsonClient = new JsonAsyncServiceClient(BaseUri);

			GetFactorialResponse response = null;
			jsonClient.SendAsync<GetFactorialResponse>(requestDto, r => response = r, 
				(r, ex) => {
					Console.WriteLine(ex.Message);
					Assert.Fail(ex.Message);
				});

			Thread.Sleep(1);

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(requestDto.ForNumber)));
		}

	}
}