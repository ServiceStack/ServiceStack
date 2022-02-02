using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class ReturnDataService : Service
    {
        public object Any(ReturnString request) => request.Data;
        public object Any(ReturnBytes request) => request.Data;
        public object Any(ReturnStream request) => request.Data;
    }
}