using ServiceStack.Service;

namespace ServiceStack.Configuration.Tests.Support
{
	public class NestedTestGateway 
	{
		public ITestGateway TestGateway { get; set; }

		public NestedTestGateway()
		{
		}

		public NestedTestGateway(ITestGateway testGateway)
		{
			this.TestGateway = testGateway;
		}
	}
}