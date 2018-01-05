using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.External | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.Xml)]
    [DataContract]
    public class SecureLiveEnvironmentRestriction { }

    [DataContract]
    public class SecureLiveEnvironmentRestrictionResponse { }

    public class SecureLiveEnvironmentRestrictionService
        : TestServiceBase<SecureLiveEnvironmentRestriction>
    {
        protected override object Run(SecureLiveEnvironmentRestriction request)
        {
            return new SecureLiveEnvironmentRestrictionResponse();
        }
    }
}