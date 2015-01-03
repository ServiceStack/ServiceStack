using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    [FallbackRoute("{PathInfo*}")]
    public class FallbackRoute
    {
        public string PathInfo { get; set; }
    }

    public class FallbackRouteService : Service
    {
        public object Any(FallbackRoute request)
        {
            if (request.PathInfo == "TestView")
            {
                return new HttpResult(base.ExecuteRequest(new CachedEcho
                    {
                        Reload = true,
                        Sentence = "Echo Result",
                    }))
                {
                    View = "TestView"
                };
            }

            return new HttpResult(request)
            {
                View = "/default.cshtml"
            };
        }
    }
}