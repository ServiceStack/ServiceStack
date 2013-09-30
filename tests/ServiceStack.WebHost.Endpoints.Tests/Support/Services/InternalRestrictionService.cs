using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Restrict(AccessTo = RequestAttributes.InternalNetworkAccess)]
	[DataContract]
	public class InternalRestriction { }

	[DataContract]
	public class IntranetRestrictionResponse { }

	public class InternalRestrictionService
		: TestServiceBase<InternalRestriction>
	{
		protected override object Run(InternalRestriction request)
		{
			return new IntranetRestrictionResponse();
		}
	}

}