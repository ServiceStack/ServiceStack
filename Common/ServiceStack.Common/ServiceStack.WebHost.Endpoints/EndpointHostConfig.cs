using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Service;
using ServiceStack.LogicFacade;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHostConfig
		: HostConfig
	{
		public IServiceHost ServiceHost { get; set; }
		public IServiceController ServiceController { get; set; }
	}

	public class HostConfig
	{
		private const string DefaultUsageExamplesBaseUri =
	    		"http://code.google.com/p/servicestack/source/browse/trunk/doc/UsageExamples";

		public HostConfig()
		{
			this.UsageExamplesBaseUri = DefaultUsageExamplesBaseUri;
			this.ServiceEndpointsMetadataConfig = new ServiceEndpointsMetadataConfig {
				DefaultMetadataUri = "Public/Metadata",
				Json = new MetadataConfig("Public/Json/SyncReply", "Public/Json/AsyncOneWay", "Public/Json/Metadata"),
				Xml = new MetadataConfig("Public/Xml/SyncReply", "Public/Xml/AsyncOneWay", "Public/Xml/Metadata"),
				Soap11 = new SoapMetadataConfig("Public/Soap11/SyncReply.svc", "Public/Soap11/AsyncOneWay.svc", "Public/Soap11/Metadata", "Public/Soap11/Wsdl"),
				Soap12 = new SoapMetadataConfig("Public/Soap12/SyncReply.svc", "Public/Soap12/AsyncOneWay.svc", "Public/Soap12/Metadata", "Public/Soap12/Wsdl"),
			};
			this.LogFactory = new NullLogFactory();
			this.EnablePortRestrictions = false;
		}

		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
		public ILogFactory LogFactory { get; set; }
		public bool EnablePortRestrictions { get; set; }
	}
}