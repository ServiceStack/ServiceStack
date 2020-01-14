using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class AnnotationsService : Service
    {
        public object Any(HelloAnnotations request) => request;
    }
}