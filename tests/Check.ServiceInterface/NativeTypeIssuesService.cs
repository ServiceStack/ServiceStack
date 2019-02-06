using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class NativeTypeIssuesService : Service
    {
        public object Any(Issue221Long request) => request;

        public object Any(TestAttributeExport request) => request;

        public object Any(RecursiveNode request) => request;
    }
}