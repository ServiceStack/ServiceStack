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
				"https://github.com/mythz/ServiceStack.Extras/blob/master/doc/UsageExamples";

		public EndpointHostConfig()
		{
			this.UsageExamplesBaseUri = DefaultUsageExamplesBaseUri;
			this.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.GetDefault();
			this.LogFactory = new NullLogFactory();
			this.EnableAccessRestrictions = true;
			this.WsdlServiceNamespace = "http://schemas.servicestack.net/types";
			this.WsdlServiceTypesNamespace = "http://schemas.servicestack.net/types";
			this.ServiceStackHandlerFactoryPath = "servicestack";
			this.DefaultContentType = ContentType.Json;
			this.ContentTypeFilter = HttpResponseFilter.Instance;
			this.RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			this.AllowJsonpRequests = true;
			this.DefaultDocuments = new List<string> {
            		"default.htm", "default.html", "index.htm", "index.html", "default.aspx", "default.ashx", 
            	};

			this.GlobalResponseHeaders = new Dictionary<string, string> { { "X-Powered-By", Env.ServerUserAgent } };
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; private set; }

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; private set; }

		public ServiceManager ServiceManager { get; set; }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
		public string UsageExamplesBaseUri { get; set; }
		public string ServiceName { get; set; }
		public string DefaultContentType { get; set; }
		public IContentTypeFilter ContentTypeFilter { get; set; }
		public bool AllowJsonpRequests { get; set; }
		public bool DebugMode { get; set; }
		public List<string> DefaultDocuments { get; private set; }

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