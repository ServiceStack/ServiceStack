using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

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
				DefaultMetadataUri = "ServiceStack/Metadata",
				Soap11 = new SoapMetadataConfig("ServiceStack/Soap11/SyncReply.svc", "ServiceStack/Soap11/AsyncOneWay.svc", "ServiceStack/Soap11/Metadata", "ServiceStack/Soap11/Wsdl"),
				Soap12 = new SoapMetadataConfig("ServiceStack/Soap12/SyncReply.svc", "ServiceStack/Soap12/AsyncOneWay.svc", "ServiceStack/Soap12/Metadata", "ServiceStack/Soap12/Wsdl"),
				Xml = new MetadataConfig("ServiceStack/Xml/SyncReply", "ServiceStack/Xml/AsyncOneWay", "ServiceStack/Xml/Metadata"),
				Json = new MetadataConfig("ServiceStack/Json/SyncReply", "ServiceStack/Json/AsyncOneWay", "ServiceStack/Json/Metadata"),
				Jsv = new MetadataConfig("ServiceStack/Jsv/SyncReply", "ServiceStack/Jsv/AsyncOneWay", "ServiceStack/Jsv/Metadata"),
			};
			this.LogFactory = new NullLogFactory();
			this.EnableAccessRestrictions = true;
			this.WsdlServiceNamespace = "http://schemas.servicestack.net/types";
			this.WsdlServiceTypesNamespace = "http://schemas.servicestack.net/types";
			this.ServiceStackHandlerFactoryPath = "ServiceStack";

			this.GlobalResponseHeaders = new Dictionary<string, string> 
				{ { "X-Powered-By", Env.ServerUserAgent } };
		}

		public ServiceManager ServiceManager { get; set; }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public string ServiceStackHandlerFactoryPath { get; set; }
		public string WsdlServiceNamespace { get; set; }
		public string WsdlServiceTypesNamespace { get; set; }
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