using System;
using System.Net;
using ServiceStack;

namespace Mvc.Core.Tests
{
    [Route("/testcache")]
    public class TestCache : IReturn<TestCache> { }

    [Route("/testcacheresult")]
    public class TestCacheOptimizedResult : IReturn<TestCacheOptimizedResult> { }

    [Route("/testcacheerror")]
    public class TestCacheError : IReturn<TestCacheError> { }

    [Route("/alwaysthrows")]
    public class AlwaysThrows { }

    public class AlwaysThrowsResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/alwaysthrowsraw")]
    public class AlwaysThrowsRaw { }

    public class CachedHtmlServices : Service
    {
        [CacheResponse(Duration = 5)]
        public object Any(TestCache request) => request;

        [DefaultView("TestCache")]
        public object Any(TestCacheOptimizedResult request)
        {
            return Request.ToOptimizedResultUsingCache(this.Cache,
                UrnId.CreateWithParts("15", request.GetType().Name), TimeSpan.FromSeconds(5), () =>
                {
                    return request;
                });
        }

        public object Any(TestCacheError request)
        {
            return Request.ToOptimizedResultUsingCache(this.Cache,
                UrnId.CreateWithParts("15", request.GetType().Name), TimeSpan.FromSeconds(20), () =>
                {
                    return request;
                });
        }

        public object Any(AlwaysThrows request)
        {
            throw new HttpError(HttpStatusCode.BadRequest, "ALWAYS THROWS");
        }

        public object Any(AlwaysThrowsRaw request)
        {
            throw new Exception(request.GetType().Name);
        }
    }
}