using NUnit.Framework;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost.Tests.Formats;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.Endpoints.Formats;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class RequestContextExtensionsTest
    {
        [Test]
        public void Can_optimize_result_with_ToOptimizedResult()
        {
            var dto = new TestDto {Name = "test"};
            
            var httpReq = new MockHttpRequest();
            httpReq.Headers.Add(HttpHeaders.AcceptEncoding, "gzip,deflate,sdch");
            httpReq.ResponseContentType = "text/html";
            var httpRes = new ViewTests.MockHttpResponse();

            var httpRequestContext = new HttpRequestContext(httpReq, httpRes, dto);

            var appHost = new TestAppHost();
            HtmlFormat.Register(appHost);
            ContentCacheManager.ContentTypeFilter = appHost.ContentTypeFilters;            
            
            object result = RequestContextExtensions.ToOptimizedResult(httpRequestContext, dto);
            Assert.IsNotNull(result);
            Assert.IsTrue(result is CompressedResult);
        }

        public class TestDto
        {
            public string Name { get; set; }
        }
    }
}