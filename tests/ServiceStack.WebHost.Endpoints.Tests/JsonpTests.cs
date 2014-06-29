using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public class JsonpTests
	{
		protected const string ListeningOn = "http://localhost:1337/";

		ExampleAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (appHost == null) return;
			appHost.Dispose();
		}

		[Test]
		public void Can_GET_single_Movie_using_RestClient_with_JSONP()
		{
            var url = ListeningOn + "all-movies/1?callback=cb";
			string response;

			var webReq = (HttpWebRequest)WebRequest.Create(url);
			webReq.Accept = "*/*";
			using (var webRes = webReq.GetResponse())
			{
                Assert.That(webRes.ContentType, Is.StringStarting(MimeTypes.JavaScript));
				response = webRes.ReadToEnd();
			}

			Assert.That(response, Is.Not.Null, "No response received");
			Console.WriteLine(response);
			Assert.That(response, Is.StringStarting("cb("));
			Assert.That(response, Is.StringEnding(")"));
			Assert.That(response.Length, Is.GreaterThan(50));
		} 
	}
}
