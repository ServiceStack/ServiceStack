using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Support;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public class JsonpTests
	{
		protected const string ListeningOn = "http://localhost:1337/";

		ExampleAppHostHttpListener appHost;

		[OneTimeSetUp]
		public void OnTestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[OneTimeTearDown]
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

#pragma warning disable CS0618
			var webReq = WebRequest.CreateHttp(url);
#pragma warning restore CS0618
			webReq.Accept = "*/*";
			using (var webRes = webReq.GetResponse())
			{
                Assert.That(webRes.ContentType, Does.StartWith(MimeTypes.JavaScript));
				response = webRes.ReadToEnd();
			}

			Assert.That(response, Is.Not.Null, "No response received");
			Console.WriteLine(response);
			Assert.That(response, Does.StartWith("cb("));
			Assert.That(response, Does.EndWith(")"));
			Assert.That(response.Length, Is.GreaterThan(50));
		}

		[Test]
		public void Can_create_Utf8_callback()
		{
			var bytes = DataCache.CreateJsonpPrefix("test");
			var fromUtf8 = bytes.FromUtf8Bytes();
			Assert.That(fromUtf8, Is.EqualTo("test("));
		}
	}
}
