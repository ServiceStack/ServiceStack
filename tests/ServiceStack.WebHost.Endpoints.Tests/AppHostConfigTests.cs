using NUnit.Framework;
using ServiceStack.Logging;
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
            appHost.Dispose();
        }
        
		[Test]
		public void Actually_uses_the_BclJsonSerializers()
		{
			var json = (ListeningOn + "login/user/pass").GetJsonFromUrl();

			json.Print();
			Assert.That(json, Is.EqualTo("{\"pwd\":\"pass\",\"uname\":\"user\"}"));
		}
	}
}