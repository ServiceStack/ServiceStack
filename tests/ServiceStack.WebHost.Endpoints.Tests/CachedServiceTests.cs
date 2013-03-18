using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Plugins.ProtoBuf;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class CachedServiceTests
	{
        ExampleAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_call_Cached_WebService_with_JSON()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);

            var response = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_ProtoBuf()
        {
            var client = new ProtoBufServiceClient(Config.ServiceStackBaseUri);

            var response = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
		public void Can_call_Cached_WebService_with_JSONP()
		{
			var url = Config.ServiceStackBaseUri.CombineWith("/cached/movies?callback=cb");
			var jsonp = url.GetJsonFromUrl();
			Assert.That(jsonp.StartsWith("cb("));
		}
	}
}