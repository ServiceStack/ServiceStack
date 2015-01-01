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
            return new HttpResult(request)
            {
                View = "/default.cshtml"
            };
        }
    }
}