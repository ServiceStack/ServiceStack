using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class CachedServiceTests
	{
		protected IRestClient CreateNewServiceClient()
		{
			return new JsonServiceClient(Config.AbsoluteBaseUri);
		}

		[Test]
		public void Can_call_Cached_WebService()
        {
			var client = CreateNewServiceClient();

			var response = client.Get<MoviesResponse>("/cached/movies");

			Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }
	}
}