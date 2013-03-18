using System;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class AppHostConfigTests
	{
		protected const string ListeningOn = "http://localhost:85/";

		TestConfigAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			appHost = new TestConfigAppHostHttpListener();
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
			appHost = null;
		}

		
		[Test]
		public void Actually_uses_the_BclJsonSerializers()
		{
			var json = (ListeningOn + "login/user/pass").GetJsonFromUrl();

			Console.WriteLine(json);
			Assert.That(json, Is.EqualTo("{\"pwd\":\"pass\",\"uname\":\"user\"}"));
		}
	}
}