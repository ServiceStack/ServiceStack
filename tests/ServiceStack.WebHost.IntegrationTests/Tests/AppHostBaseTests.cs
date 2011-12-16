using System.Net;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class AppHostBaseTests
	{
		private const string BasePath = "http://localhost/ServiceStack.WebHost.IntegrationTests/";
		private const string ServiceStackUrl = BasePath + "api/";

		[Test]
		public void Root_path_redirects_to_metadata_page()
		{
			var html = ServiceStackUrl.DownloadUrl();
			Assert.That(html.Contains("The following operations are supported."));
		}

		[Test]
		public void Can_download_webpage_html_page()
		{
			var html = (BasePath + "webpage.html").DownloadUrl();
			Assert.That(html.Contains("Default index ServiceStack.WebHost.Endpoints.Tests page"));
		}

		[Test]
		public void Gets_404_on_non_existant_page()
		{
			var webRes = (BasePath + "nonexistant.html").GetErrorResponse();
			Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}

		[Test]
		public void Gets_404_3_on_page_with_non_whitelisted_extension()
		{
			var webRes = (BasePath + "webpage.forbidden").GetErrorResponse();
			Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}
		 
	}
}