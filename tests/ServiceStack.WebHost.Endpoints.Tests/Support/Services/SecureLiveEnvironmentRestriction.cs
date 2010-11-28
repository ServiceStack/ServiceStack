using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.External | EndpointAttributes.Secure | EndpointAttributes.HttpPost | EndpointAttributes.Xml)]
	[DataContract]
	public class SecureLiveEnvironmentRestriction { }

	[DataContract]
	public class SecureLiveEnvironmentRestrictionResponse { }

	public class SecureLiveEnvironmentRestrictionService
		: TestServiceBase<SecureLiveEnvironmentRestriction>
	{
		protected override object Run(SecureLiveEnvironmentRestriction request)
		{
			return new SecureLiveEnvironmentRestrictionResponse();
		}
	}
}