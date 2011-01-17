using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class AppHostListenerBaseTests
	{
		private const string ListeningOn = "http://localhost:82/";
		ExampleAppHostHttpListener appHost;

		static AppHostListenerBaseTests()
		{
			LogManager.LogFactory = new ConsoleLogFactory();
		}

		[TestFixtureSetUp]
		public void OnTestFixtureStartUp() 
		{
			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);

			System.Console.WriteLine("ExampleAppHost Created at {0}, listening on {1}",
			                         DateTime.Now, ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
			appHost = null;
		}

		[Test]
		public void Can_call_GetFactorial_WebService()
		{
			var client = new XmlServiceClient(ListeningOn);
			var request = new GetFactorial { ForNumber = 3 };
			var response = client.Send<GetFactorialResponse>(request);

			Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
		}

		[Test]
		public void Can_call_jsv_debug_on_GetFactorial_WebService()
		{
			const string url = ListeningOn + "jsv/syncreply/GetFactorial?ForNumber=3&debug=true";
			var contents = new StreamReader(WebRequest.Create(url).GetResponse().GetResponseStream()).ReadToEnd();
			
			Console.WriteLine("JSV DEBUG: " + contents);

			Assert.That(contents, Is.Not.Null);
		}

		[Test]
		public void Calling_missing_web_service_does_not_break_HttpListener()
		{
			int errorCount = 0;
			try
			{
				var call1 = new StreamReader(WebRequest.Create(ListeningOn).GetResponse().GetResponseStream()).ReadToEnd();
			}
			catch (Exception ex)
			{
				errorCount++;
				Console.WriteLine("Error [{0}]: {1}", ex.GetType().Name, ex.Message);
			}
			try
			{
				var call2 = new StreamReader(WebRequest.Create(ListeningOn).GetResponse().GetResponseStream()).ReadToEnd();
			}
			catch (Exception ex)
			{
				errorCount++;
				Console.WriteLine("Error [{0}]: {1}", ex.GetType().Name, ex.Message);
			}

			Assert.That(errorCount, Is.EqualTo(2));
		}
	}
}