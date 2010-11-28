using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{

	[Service(EndpointAttributes.InternalNetworkAccess | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.Xml)]
	[DataContract]
	public class SecureDevEnvironmentRestriction { }

	[DataContract]
	public class SecureDevEnvironmentRestrictionResponse { }

	public class SecureDevEnvironmentRestrictionService
		: TestServiceBase<SecureDevEnvironmentRestriction>
	{
		protected override object Run(SecureDevEnvironmentRestriction request)
		{
			return new SecureDevEnvironmentRestrictionResponse();
		}
	}

}