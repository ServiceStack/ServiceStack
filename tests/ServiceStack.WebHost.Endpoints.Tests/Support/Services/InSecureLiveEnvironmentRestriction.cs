using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Restrict(RequestAttributes.External | RequestAttributes.InSecure | RequestAttributes.HttpPost | RequestAttributes.Xml)]
	[DataContract]
	public class InSecureLiveEnvironmentRestriction { }

	[DataContract]
	public class InSecureLiveEnvironmentRestrictionResponse { }

	public class InSecureLiveEnvironmentRestrictionService
		: TestServiceBase<InSecureLiveEnvironmentRestriction>
	{
		protected override object Run(InSecureLiveEnvironmentRestriction request)
		{
			return new InSecureLiveEnvironmentRestrictionResponse();
		}
	}
}