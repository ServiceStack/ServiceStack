using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

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
				Soap11 = new SoapMetadataConfig("Public/Soap11/SyncReply.svc", "Public/Soap11/AsyncOneWay.svc", "Public/Soap11/Metadata", "Public/Soap11/Wsdl"),
				Soap12 = new SoapMetadataConfig("Public/Soap12/SyncReply.svc", "Public/Soap12/AsyncOneWay.svc", "Public/Soap12/Metadata", "Public/Soap12/Wsdl"),
				Xml = new MetadataConfig("Public/Xml/SyncReply", "Public/Xml/AsyncOneWay", "Public/Xml/Metadata"),
				Json = new MetadataConfig("Public/Json/SyncReply", "Public/Json/AsyncOneWay", "Public/Json/Metadata"),
				Jsv = new MetadataConfig("Public/Jsv/SyncReply", "Public/Jsv/AsyncOneWay", "Public/Jsv/Metadata"),
			};
			this.LogFactory = new NullLogFactory();
			this.EnableAccessRestrictions = true;
			this.DefaultContentType = ContentType.Json;

			this.GlobalResponseHeaders = new Dictionary<string, string> 
				{ { "X-Powered-By", Env.ServerUserAgent } };
		}

		public IServiceController ServiceController { get; set; }
		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public string DefaultContentType { get; set; }
		public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
		public ILogFactory LogFactory { get; set; }
		public bool EnableAccessRestrictions { get; set; }
		public bool UseBclJsonSerializers { get; set; }
		public Dictionary<string, string> GlobalResponseHeaders { get; set; }

		private string defaultOperationNamespace;
		public string DefaultOperationNamespace
		{
			get
			{
				if (this.defaultOperationNamespace == null)
				{
					this.defaultOperationNamespace = GetDefaultNamespace();
				}
				return this.defaultOperationNamespace;
			}
			set
			{
				this.defaultOperationNamespace = value;
			}
		}

		private string GetDefaultNamespace()
		{
			if (!string.IsNullOrEmpty(this.defaultOperationNamespace)
				|| this.ServiceController == null) return null;

			foreach (var operationType in this.ServiceController.OperationTypes)
			{
				var attrs = operationType.GetCustomAttributes(
					typeof(DataContractAttribute), false);

				if (attrs.Length <= 0) continue;

				var attr = (DataContractAttribute)attrs[0];

				if (string.IsNullOrEmpty(attr.Namespace)) continue;

				return attr.Namespace;
			}

			return null;
		}

	}
}