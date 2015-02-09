using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class AppHostConfigTests
	{
		protected const string ListeningOn = "http://localhost:1337/";

		ServiceStackHost appHost;

		[TestFixtureSetUp]
        public void TestFixtureSetUp()
		{
			appHost = new TestConfigAppHostHttpListener()
			    .Init()
			    .Start(ListeningOn);
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
