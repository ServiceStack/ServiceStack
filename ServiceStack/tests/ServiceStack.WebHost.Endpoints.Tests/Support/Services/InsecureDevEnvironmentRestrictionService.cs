using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.InternalNetworkAccess | RequestAttributes.InSecure | RequestAttributes.HttpPost | RequestAttributes.Xml)]
    [DataContract]
    public class InSecureDevEnvironmentRestriction { }

    [DataContract]
    public class InsecureDevEnvironmentRestrictionResponse { }

    public class InsecureDevEnvironmentRestrictionService
        : TestServiceBase<InSecureDevEnvironmentRestriction>
    {
        protected override object Run(InSecureDevEnvironmentRestriction request)
        {
            return new InsecureDevEnvironmentRestrictionResponse();
        }
    }

}