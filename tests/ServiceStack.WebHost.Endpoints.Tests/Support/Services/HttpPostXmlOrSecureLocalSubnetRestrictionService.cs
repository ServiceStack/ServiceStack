using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure, EndpointAttributes.HttpPost | EndpointAttributes.Xml)]
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