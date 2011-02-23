using System;
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
				"https://github.com/ServiceStack/ServiceStack.Extras/blob/master/doc/UsageExamples";

		private static EndpointHostConfig instance;
		public static EndpointHostConfig Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new EndpointHostConfig
					{
						UsageExamplesBaseUri = DefaultUsageExamplesBaseUri,
						LogFactory = new NullLogFactory(),
						EnableAccessRestrictions = true,
						WsdlServiceNamespace = "http://schemas.servicestack.net/types",
						WsdlServiceTypesNamespace = "http://schemas.servicestack.net/types",
						ServiceStackHandlerFactoryPath = "servicestack",
						DefaultContentType = ContentType.Json,
						AllowJsonpRequests = true,
						DefaultDocuments = new List<string> {
							"default.htm",
							"default.html",
							"index.htm",
							"index.html",
							"default.aspx",
							"default.ashx",
						},
						GlobalResponseHeaders = new Dictionary<string, string> { { "X-Powered-By", Env.ServerUserAgent } },
						IgnoreFormatsInMetadata = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase),
						AllowFileExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
						{
							"js", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", 
							"jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", 
							"avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv", 
							"flv", "xap", "xaml"
						}
					};
				}
				return instance;
			}
		}

		public EndpointHostConfig()
		{
			if (instance == null) return;

			//Get a copy of the singleton already partially configured
			this.UsageExamplesBaseUri = instance.UsageExamplesBaseUri;
			this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
			this.LogFactory = instance.LogFactory;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
			this.WsdlServiceTypesNamespace = instance.WsdlServiceTypesNamespace;
			this.ServiceStackHandlerFactoryPath = instance.ServiceStackHandlerFactoryPath;
			this.DefaultContentType = instance.DefaultContentType;
			this.AllowJsonpRequests = instance.AllowJsonpRequests;
			this.DefaultDocuments = instance.DefaultDocuments;
			this.GlobalResponseHeaders = instance.GlobalResponseHeaders;
			this.IgnoreFormatsInMetadata = instance.IgnoreFormatsInMetadata;
			this.AllowFileExtensions = instance.AllowFileExtensions;
		}

		public ServiceManager ServiceManager { get; set; }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public string DefaultContentType { get; set; }
		public bool AllowJsonpRequests { get; set; }
		public bool DebugMode { get; set; }
		public List<string> DefaultDocuments { get; private set; }

		public HashSet<string> IgnoreFormatsInMetadata { get; set; }

		public HashSet<string> AllowFileExtensions { get; set; }

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
