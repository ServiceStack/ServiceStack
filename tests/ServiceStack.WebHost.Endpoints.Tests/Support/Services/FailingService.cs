using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [DataContract]
    public class FailingRequest { }

    [DataContract]
    public class FailingRequestResponse { }

    public class FailingService : IService<FailingRequest>
    {
        private void ThisMethodAlwaysThrowsAnError(FailingRequest request)
        {
            throw new System.ArgumentException("Failure");
        }
        
        public object Execute(FailingRequest request)
        {
            ThisMethodAlwaysThrowsAnError(request);
            return new FailingRequestResponse();
        }
    }
}