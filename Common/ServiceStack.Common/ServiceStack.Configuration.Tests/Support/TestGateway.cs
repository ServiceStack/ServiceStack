using ServiceStack.Service;

namespace ServiceStack.Configuration.Tests.Support
{
	public class TestGateway : ITestGateway
	{
		public IServiceClient ServiceClient { get; set; }

		public TestGateway(IServiceClient serviceClient)
		{
			this.ServiceClient = serviceClient;
		}
	}
}