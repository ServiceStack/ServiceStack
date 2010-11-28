using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.InternalNetworkAccess | EndpointAttributes.InSecure | EndpointAttributes.HttpPost | EndpointAttributes.Xml)]
	[DataContract]
	public class InSecureDevEnvironmentRestriction { }

	[DataContract]
	public class InsecureDevEnvironmentRestrictionResponse { }

	public class InsecureDevEnvironmentRestrictionService
		: TestServiceBase<InSecureDevEnvironmentRestriction>
	{
		protected override object Run(InSecureDevEnvironmentRestriction request)
		{
			return new InsecureDevEnvironmentRestrictionResponse();
		}
	}

}