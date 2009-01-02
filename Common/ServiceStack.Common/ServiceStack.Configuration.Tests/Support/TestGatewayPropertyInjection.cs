using ServiceStack.Service;

namespace ServiceStack.Configuration.Tests.Support
{
	public class TestGatewayPropertyInjection : ITestGateway
	{
		public IServiceClient ServiceClient { get; set; }
	}
}