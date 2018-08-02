using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class SwaggerServices : Service
    {
        public object Any(SwaggerVersionTest request) => request;

        public object Any(SwaggerRangeTest request) => request;
    }
}