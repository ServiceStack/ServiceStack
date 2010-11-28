using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.Xml)]
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