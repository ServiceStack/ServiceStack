using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[Service(EndpointAttributes.InternalNetworkAccess)]
	[DataContract]
	public class InternalRestriction { }

	[DataContract]
	public class IntranetRestrictionResponse { }
}