using NUnit.Framework;
using ServiceStack.Service;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	public abstract class EndpointTestsBase
	{
		protected abstract IServiceClient CreateNewServiceClient();
	}

	[TestFixture]
	public class XmlTests
	{
	}
}