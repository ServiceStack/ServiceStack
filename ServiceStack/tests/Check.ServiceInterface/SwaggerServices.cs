using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class SwaggerServices : Service
    {
        public object Any(SwaggerVersionTest request) => request;

        public object Any(SwaggerRangeTest request) => request;

        public object Any(SwaggerDescTest request) => request;

        public object Any(SwaggerSearch request) => new EmptyResponse();
    }
}