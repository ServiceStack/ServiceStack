using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Plugins.ProtoBuf;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class CachedServiceTests
	{
        [TestFixtureSetUp]
        public void OnBeforeEachTest()
        {
            var jsonClient = new JsonServiceClient(Config.ServiceStackBaseUri);
            jsonClient.Post<ResetMoviesResponse>("reset-movies", new ResetMovies());
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
			var jsonp = url.DownloadJsonFromUrl();
			Assert.That(jsonp.StartsWith("cb("));
		}
	}
}