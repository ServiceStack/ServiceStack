using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[Service(EndpointAttributes.LocalSubnet | EndpointAttributes.Secure, EndpointAttributes.HttpPost | EndpointAttributes.Xml)]
	[DataContract]
	public class HttpPostXmlOrSecureLocalSubnetRestriction { }

	[DataContract]
	public class HttpPostXmlOrSecureLocalSubnetRestrictionResponse { }
}