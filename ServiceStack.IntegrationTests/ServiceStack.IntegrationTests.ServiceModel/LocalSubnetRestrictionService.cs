using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[Service(EndpointAttributes.LocalSubnet)]
	[DataContract]
	public class LocalSubnetRestriction { }

	[DataContract]
	public class LocalSubnetRestrictionResponse { }
}