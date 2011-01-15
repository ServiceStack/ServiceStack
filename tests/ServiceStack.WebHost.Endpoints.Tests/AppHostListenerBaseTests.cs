using System;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class AppHostListenerBaseTests
	{
		private const string ListeningOn = "http://localhost:82/";

		[Test]
		public void Can_start_Listener_and_call_GetFactorial_WebService()
		{
			var appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);

			System.Console.WriteLine("ExampleAppHost Created at {0}, listening on {1}",
				DateTime.Now, ListeningOn);

			var client = new XmlServiceClient(ListeningOn);
			var request = new GetFactorial { ForNumber = 3 };
			var response = client.Send<GetFactorialResponse>(request);

			Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));

			appHost.Dispose();
		}
	}
}