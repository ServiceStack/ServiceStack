using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Service;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHostConfig
	{
		private const string DefaultUsageExamplesBaseUri =
	    		"http://code.google.com/p/servicestack/source/browse/trunk/doc/UsageExamples";

		public EndpointHostConfig()
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

		public IServiceController ServiceController { get; set; }
		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
		public ILogFactory LogFactory { get; set; }
		public bool EnablePortRestrictions { get; set; }
	}
}