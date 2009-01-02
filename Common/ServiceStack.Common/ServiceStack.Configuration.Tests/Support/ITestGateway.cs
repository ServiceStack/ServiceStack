using ServiceStack.Service;

namespace ServiceStack.Configuration.Tests.Support
{	
	public interface ITestGateway
	{
		IServiceClient ServiceClient { get; set; }
	}
}