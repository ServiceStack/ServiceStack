using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.Secure | EndpointAttributes.LocalSubnet)]
	[DataContract]
	public class SecureLocalSubnetRestriction { }

	[DataContract]
	public class SecureLocalSubnetRestrictionResponse { }

	public class SecureLocalSubnetRestrictionService
		: TestServiceBase<SecureLocalSubnetRestriction>
	{
		protected override object Run(SecureLocalSubnetRestriction request)
		{
			return new SecureLocalSubnetRestrictionResponse();
		}
	}

}