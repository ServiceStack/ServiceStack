using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Restrict(RequestAttributes.LocalSubnet | RequestAttributes.Secure | RequestAttributes.HttpPost | RequestAttributes.Xml)]
    [DataContract]
    public class HttpPostXmlAndSecureLocalSubnetRestriction { }

    [DataContract]
    public class HttpPostXmlAndSecureLocalSubnetRestrictionResponse { }

    public class HttpPostXmlAndSecureLocalSubnetRestrictionService
        : TestServiceBase<HttpPostXmlAndSecureLocalSubnetRestriction>
    {
        protected override object Run(HttpPostXmlAndSecureLocalSubnetRestriction request)
        {
            return new HttpPostXmlAndSecureLocalSubnetRestrictionResponse();
        }
    }

}