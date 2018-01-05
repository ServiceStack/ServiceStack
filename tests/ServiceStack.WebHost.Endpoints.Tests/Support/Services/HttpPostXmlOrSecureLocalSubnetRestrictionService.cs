using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.LocalSubnet | RequestAttributes.Secure, RequestAttributes.HttpPost | RequestAttributes.Xml)]
    [DataContract]
    public class HttpPostXmlOrSecureLocalSubnetRestriction { }

    [DataContract]
    public class HttpPostXmlOrSecureLocalSubnetRestrictionResponse { }

    public class HttpPostXmlOrSecureLocalSubnetRestrictionService
        : TestServiceBase<HttpPostXmlOrSecureLocalSubnetRestriction>
    {
        protected override object Run(HttpPostXmlOrSecureLocalSubnetRestriction request)
        {
            return new HttpPostXmlOrSecureLocalSubnetRestrictionResponse();
        }
    }
}