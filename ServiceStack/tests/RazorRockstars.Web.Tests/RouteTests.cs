using NUnit.Framework;
using ServiceStack;
using ServiceStack.Host.Handlers;

namespace RazorRockstars.Web.Tests
{
    public class GetRouteInfoResponse
    {
        public string BaseUrl { get; set; }
        public string ResolvedUrl { get; set; }
    }

    public class RouteInfoPathTests
    {
        [Test]
        public void ApiPath_returns_BaseUrl()
        {
            var url = Config.ServiceStackBaseUri;

            var reqInfoResponse = url.AddQueryParam("debug", "requestinfo")
                .GetJsonFromUrl().FromJson<RequestInfoResponse>();
            Assert.That(reqInfoResponse.ApplicationBaseUrl, Is.EqualTo(url));
            Assert.That(reqInfoResponse.ResolveAbsoluteUrl, Is.EqualTo(url + "/resolve"));

            var response = url.CombineWith("/routeinfo").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
            Assert.That(response.BaseUrl, Is.EqualTo(url));
            Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

            response = url.CombineWith("/routeinfo/dir").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
            Assert.That(response.BaseUrl, Is.EqualTo(url));
            Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

            response = url.CombineWith("/routeinfo/dir/sub").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
            Assert.That(response.BaseUrl, Is.EqualTo(url));
            Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));
        }
    }
}