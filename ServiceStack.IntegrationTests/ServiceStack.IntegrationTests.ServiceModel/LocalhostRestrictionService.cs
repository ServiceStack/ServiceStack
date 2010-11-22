using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceModel
{
	[Service(EndpointAttributes.Localhost)]
	[DataContract]
	public class LocalhostRestriction { }

	[DataContract]
	public class LocalhostRestrictionResponse { }
}