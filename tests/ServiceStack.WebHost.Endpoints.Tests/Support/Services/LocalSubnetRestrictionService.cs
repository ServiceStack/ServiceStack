using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Restrict(RequestAttributes.LocalSubnet)]
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