using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[Restrict(RequestAttributes.Localhost)]
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


    public class LocalhostRestrictionOnService : IReturn<Response> { }

    [Restrict(LocalhostOnly = true)]
    public class LocalHostOnService : Service
    {
        public Response Any(LocalhostRestrictionOnService request)
        {
            return new Response();
        }
    }

}
