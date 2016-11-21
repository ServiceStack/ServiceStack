using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.Secure | RequestAttributes.LocalSubnet)]
    [DataContract]
    public class SecureLocalSubnetRestriction { }

    [DataContract]
    public class SecureLocalSubnetRestrictionResponse { }

    public class SecureLocalSubnetRestrictionService
        : TestServiceBase<SecureLocalSubnetRestriction>
    {
        protected override object Run(SecureLocalSubnetRestriction request)
        {
            return new SecureLocalSubnetRestrictionResponse();
        }
    }

}