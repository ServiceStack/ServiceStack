using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [DataContract]
    public class Nested { }
    [DataContract]
    public class NestedResponse { }

    public class NestedService
        : TestServiceBase<Nested>
    {
        protected override object Run(Nested request)
        {
            return new NestedResponse();
        }
    }

}