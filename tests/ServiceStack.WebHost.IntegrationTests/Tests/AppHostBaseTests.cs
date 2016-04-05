using System.Net;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class AppHostBaseTests
    {
        private const string BasePath = Config.AbsoluteBaseUri;

        [Test]
        public void Can_download_metadata_page()
        {
            var html = Config.ServiceStackBaseUri.CombineWith("metadata").GetStringFromUrl();
            Assert.That(html.Contains("The following operations are supported."));
        }

        [Test]
        public void Can_download_webpage_html_page()
        {
            var html = (BasePath + "webpage.html").GetStringFromUrl();
            Assert.That(html.Contains("Default index ServiceStack.WebHost.Endpoints.Tests page"));
        }

        [Test]
        public void Gets_404_on_non_existant_page()
        {
            var webRes = (BasePath + "nonexistant.html").GetErrorResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void Gets_403_on_page_with_non_whitelisted_extension()
        {
            var webRes = (BasePath + "api/webpage.forbidden").GetErrorResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

    }
}