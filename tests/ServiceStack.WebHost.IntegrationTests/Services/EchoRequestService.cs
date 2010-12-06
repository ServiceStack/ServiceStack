using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.IntegrationTests.Operations;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	public class EchoRequestService 
		: ServiceBase<EchoRequest>
	{
		protected override object Run(EchoRequest request)
		{
			return request;
		}
	}
}