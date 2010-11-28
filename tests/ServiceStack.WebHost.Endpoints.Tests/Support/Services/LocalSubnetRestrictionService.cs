using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.LocalSubnet)]
	[DataContract]
	public class LocalSubnetRestriction { }

	[DataContract]
	public class LocalSubnetRestrictionResponse { }

	public class LocalSubnetRestrictionService
		: TestServiceBase<LocalSubnetRestriction>
	{
		protected override object Run(LocalSubnetRestriction request)
		{
			return new LocalSubnetRestrictionResponse();
		}
	}

}