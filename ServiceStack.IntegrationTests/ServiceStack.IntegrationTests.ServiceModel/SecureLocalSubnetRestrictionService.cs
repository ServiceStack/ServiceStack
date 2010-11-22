using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[Service(EndpointAttributes.Secure | EndpointAttributes.LocalSubnet)]
	[DataContract]
	public class SecureLocalSubnetRestriction { }

	[DataContract]
	public class SecureLocalSubnetRestrictionResponse { }
}