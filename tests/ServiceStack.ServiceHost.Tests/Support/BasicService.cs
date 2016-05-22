using System.Runtime.Serialization;

namespace ServiceStack.ServiceHost.Tests.Support
{
    [DataContract]
    public class EmptyRequest { }

    [DataContract]
    public class EmptyRequestResponse { }

    public class BasicService : IService
    {
        public object Any(EmptyRequest request)
        {
            return new EmptyRequestResponse();
        }
    }
}