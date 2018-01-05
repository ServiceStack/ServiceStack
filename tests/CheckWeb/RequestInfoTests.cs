using System.Net;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Host.Handlers;

namespace CheckWeb
{
    public class RequestInfoServices : Service
    {
    }

    public partial class RequestInfoTests
    {
        public string BaseUrl = "http://localhost:55799/";

        private RequestInfoResponse GetRequestInfoForPath(string path)
        {
            var url = BaseUrl.CombineWith(path).AddQueryParam("debug", "requestinfo");
            var json = url.GetJsonFromUrl();
            var info = json.FromJson<RequestInfoResponse>();
            return info;
        }

        private void AssertHasContent(string pathInfo, string accept, string containsContent)
        {
            var url = BaseUrl.CombineWith(pathInfo);
            var content = url.GetStringFromUrl(accept: accept);
            Assert.That(content, Does.Contain(containsContent));
        }

        [Test]
        public void Does_return_expected_content()
        {
            AssertHasContent("metadata", MimeTypes.Html, "The following operations are supported");
            AssertHasContent("metadata/", MimeTypes.Html, "The following operations are supported");
            AssertHasContent("dir", MimeTypes.Html, "<h1>dir/index.html</h1>");
            AssertHasContent("dir/", MimeTypes.Html, "<h1>dir/index.html</h1>");
            AssertHasContent("dir/sub", MimeTypes.Html, "<h1>dir/sub/index.html</h1>");
            AssertHasContent("dir/sub/", MimeTypes.Html, "<h1>dir/sub/index.html</h1>");
            AssertHasContent("swagger-ui", MimeTypes.Html, "<title>Swagger UI</title>");
            AssertHasContent("swagger-ui/", MimeTypes.Html, "<title>Swagger UI</title>");
        }

        [Test]
        public void Does_have_correct_path_info()
        {
            Assert.That(GetRequestInfoForPath("dir/").PathInfo, Is.EqualTo("/dir/"));
            Assert.That(GetRequestInfoForPath("dir/sub/").PathInfo, Is.EqualTo("/dir/sub/"));
            Assert.That(GetRequestInfoForPath("dir/sub/").PathInfo, Is.EqualTo("/dir/sub/"));
            Assert.That(GetRequestInfoForPath("swagger-ui/").PathInfo, Is.EqualTo("/swagger-ui/"));
        }
    }

    public partial class RequestInfoTests
    {
        private void DoesRedirectToRemoveTrailingSlash(string dirWIthoutSlash)
        {
            BaseUrl.CombineWith(dirWIthoutSlash)
                .GetStringFromUrl(accept: MimeTypes.Html,
                    requestFilter: req => req.AllowAutoRedirect = false,
                    responseFilter: res =>
                    {
                        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                        Assert.That(res.Headers[HttpHeaders.Location],
                            Is.EqualTo(BaseUrl.CombineWith(dirWIthoutSlash + "/")));
                    });
        }

        private void DoesRedirectToAddTrailingSlash(string dirWithoutSlash)
        {
            BaseUrl.CombineWith(dirWithoutSlash)
                .GetStringFromUrl(accept: MimeTypes.Html,
                    requestFilter: req => req.AllowAutoRedirect = false,
                    responseFilter: res =>
                    {
                        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                        Assert.That(res.Headers[HttpHeaders.Location],
                            Is.EqualTo(BaseUrl.CombineWith(dirWithoutSlash.TrimEnd('/'))));
                    });
        }

        [Test]
        public void Does_redirect_dirs_without_trailing_slash()
        {
            DoesRedirectToRemoveTrailingSlash("dir");
            DoesRedirectToRemoveTrailingSlash("dir/sub");
            DoesRedirectToRemoveTrailingSlash("swagger-ui");
        }

        [Test]
        public void Does_redirect_metadata_page_to_without_slash()
        {
            DoesRedirectToAddTrailingSlash("metadata/");
        }
    }
}