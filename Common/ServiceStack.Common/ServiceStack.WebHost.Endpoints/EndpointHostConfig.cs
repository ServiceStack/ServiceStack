using ServiceStack.Service;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceModel;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHostConfig
	{
		private const string USAGE_EXAMPLES_BASE_URI =
    		"http://code.google.com/p/servicestack/source/browse/trunk/ExampleProjects/ServiceStack.Sakila/ServiceStack.UsageExamples";

		public EndpointHostConfig()
		{
			this.UsageExamplesBaseUri = USAGE_EXAMPLES_BASE_URI;
		}

		public string UsageExamplesBaseUri { get; set; }
		public IServiceHost ServiceHost { get; set; }
		public IServiceController ServiceController { get; set; }
		public IServiceModelFinder ServiceModelFinder { get; set; }
		public string OperationsNamespace { get; set; }
		public string ServiceName { get; set; }
	}
}