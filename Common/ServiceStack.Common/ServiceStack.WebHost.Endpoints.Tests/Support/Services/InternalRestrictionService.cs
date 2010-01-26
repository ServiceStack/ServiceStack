using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Service(EndpointAttributes.Internal)]
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