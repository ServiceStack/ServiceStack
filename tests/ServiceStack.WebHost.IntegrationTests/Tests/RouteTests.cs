using NUnit.Framework;
using ServiceStack.Host.Handlers;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/routeinfo/{Path*}")]
    public class GetRouteInfo
    {
        public string Path { get; set; }
    }

    public class GetRouteInfoResponse
    {
        public string BaseUrl { get; set; }
        public string ResolvedUrl { get; set; }
    }

    public class RouteInfoService : Service
    {
        public object Any(GetRouteInfo request)
        {
            return new GetRouteInfoResponse
            {
                BaseUrl = base.Request.GetBaseUrl(),
                ResolvedUrl = base.Request.ResolveAbsoluteUrl("~/resolved")
            };
        }
    }

    public class RouteInfoPathTests
    {
        [Test]
        public void ApiPath_returns_BaseUrl()
        {
            var url = Config.AbsoluteBaseUri.AppendPath("api");

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