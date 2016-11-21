using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.InternalNetworkAccess | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.Xml)]
    [DataContract]
    public class SecureDevEnvironmentRestriction { }

    [DataContract]
    public class SecureDevEnvironmentRestrictionResponse { }

    public class SecureDevEnvironmentRestrictionService
        : TestServiceBase<SecureDevEnvironmentRestriction>
    {
        protected override object Run(SecureDevEnvironmentRestriction request)
        {
            return new SecureDevEnvironmentRestrictionResponse();
        }
    }
}