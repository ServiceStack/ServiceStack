using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.Localhost)]
	[DataContract]
	public class LocalhostRestriction { }

	[DataContract]
	public class LocalhostRestrictionResponse { }

	public class LocalhostRestrictionService
		: TestServiceBase<LocalhostRestriction>
	{
		protected override object Run(LocalhostRestriction request)
		{
			return new LocalhostRestrictionResponse();
		}
	}

}
