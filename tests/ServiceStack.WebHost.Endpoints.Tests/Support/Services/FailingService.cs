using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [DataContract]
    public class FailingRequest { }

    [DataContract]
    public class FailingRequestResponse { }

    public class FailingService : IService
    {
        private void ThisMethodAlwaysThrowsAnError(FailingRequest request)
        {
            throw new System.ArgumentException("Failure");
        }
        
        public object Any(FailingRequest request)
        {
            ThisMethodAlwaysThrowsAnError(request);
            return new FailingRequestResponse();
        }
    }
}