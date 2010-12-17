using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
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
				DefaultMetadataUri = "servicestack/metadata",
				Soap11 = new SoapMetadataConfig("servicestack/soap11/syncreply.svc", "servicestack/soap11/asynconeway.svc", "servicestack/soap11/metadata", "soap11"),
				Soap12 = new SoapMetadataConfig("servicestack/soap12/syncreply.svc", "servicestack/soap12/asynconeway.svc", "servicestack/soap12/metadata", "soap12"),
				Xml = new MetadataConfig("servicestack/xml/syncreply", "servicestack/xml/asynconeway", "servicestack/xml/metadata"),
				Json = new MetadataConfig("servicestack/json/syncreply", "servicestack/json/asynconeway", "servicestack/json/metadata"),
				Jsv = new MetadataConfig("servicestack/jsv/syncreply", "servicestack/jsv/asynconeway", "servicestack/jsv/metadata"),
			};
			this.LogFactory = new NullLogFactory();
			this.EnableAccessRestrictions = true;
			this.WsdlServiceNamespace = "http://schemas.servicestack.net/types";
			this.WsdlServiceTypesNamespace = "http://schemas.servicestack.net/types";
			this.ServiceStackHandlerFactoryPath = "servicestack";
			this.DefaultContentType = ContentType.Json;
			this.ContentTypeFilter = HttpResponseFilter.Instance;
			this.AllowJsonpRequests = true;

			this.GlobalResponseHeaders = new Dictionary<string, string> 
				{ { "X-Powered-By", Env.ServerUserAgent } };
		}

		public ServiceManager ServiceManager { get; set; }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public string DefaultContentType { get; set; }
		public IContentTypeFilter ContentTypeFilter { get; set; }
		public bool AllowJsonpRequests { get; set; }
		
		private string serviceStackHandlerFactoryPath;
		public string ServiceStackHandlerFactoryPath
		{
			get { return serviceStackHandlerFactoryPath; }
			set { serviceStackHandlerFactoryPath = value != null ? value.ToLower() : null; }
		}

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