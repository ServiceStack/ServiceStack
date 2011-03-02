using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;
using System.Web.Configuration;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHostConfig
	{
		public static bool SkipPathValidation = false;
		public static string ServiceStackPath = null;

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
						ServiceStackHandlerFactoryPath = ServiceStackPath,
						MetadataRedirectPath = null,
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
						},
						DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
						DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
					};

					if (instance.ServiceStackHandlerFactoryPath == null)
					{
						InferHttpHandlerPath();
					}
				}
				return instance;
			}
		}

		public EndpointHostConfig()
		{
			if (instance == null) return;

			//Get a copy of the singleton already partially configured
			this.UsageExamplesBaseUri = instance.UsageExamplesBaseUri;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
			this.LogFactory = instance.LogFactory;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
			this.WsdlServiceTypesNamespace = instance.WsdlServiceTypesNamespace;
			this.DefaultRedirectPath = instance.DefaultRedirectPath;
			this.MetadataRedirectPath = instance.MetadataRedirectPath;
			this.ServiceStackHandlerFactoryPath = instance.ServiceStackHandlerFactoryPath;
			this.DefaultContentType = instance.DefaultContentType;
			this.AllowJsonpRequests = instance.AllowJsonpRequests;
			this.DefaultDocuments = instance.DefaultDocuments;
			this.GlobalResponseHeaders = instance.GlobalResponseHeaders;
			this.IgnoreFormatsInMetadata = instance.IgnoreFormatsInMetadata;
			this.AllowFileExtensions = instance.AllowFileExtensions;
		}

		private static void InferHttpHandlerPath()
		{
			try
			{
				//Read the user-defined path in the Web.Config
				var config = WebConfigurationManager.OpenWebConfiguration("~/");
				var handlersSection = config.GetSection("system.web/httpHandlers") as HttpHandlersSection;
				if (handlersSection != null)
				{
					for (var i = 0; i < handlersSection.Handlers.Count; i++)
					{
						var httpHandler = handlersSection.Handlers[i];
						if (!httpHandler.Type.StartsWith("ServiceStack")) continue;

						var handlerPath = httpHandler.Path.Replace("*", "");
						instance.MetadataRedirectPath = PathUtils.CombinePaths(
							handlerPath, "metadata");
						instance.ServiceStackHandlerFactoryPath = string.IsNullOrEmpty(handlerPath)
							? null : handlerPath;

						break;
					}
				}

				if (instance.MetadataRedirectPath == null)
				{
					foreach (ConfigurationLocation location in config.Locations)
					{
						var locationPath = (location.Path ?? "").ToLower();
						System.Configuration.Configuration locConfig = location.OpenConfiguration();
						handlersSection = locConfig.GetSection("system.web/httpHandlers") as HttpHandlersSection;
						if (handlersSection == null) continue;

						for (var i = 0; i < handlersSection.Handlers.Count; i++)
						{
							var httpHandler = handlersSection.Handlers[i];
							if (!httpHandler.Type.StartsWith("ServiceStack")) continue;

							instance.ServiceStackHandlerFactoryPath = locationPath;
							instance.MetadataRedirectPath = PathUtils.CombinePaths(
								instance.ServiceStackHandlerFactoryPath, "metadata");

							break;
						}
					}
				}

				if (!SkipPathValidation && instance.MetadataRedirectPath == null)
				{
					throw new ConfigurationErrorsException(
						"Unable to infer ServiceStack's <httpHandler.Path/> from the Web.Config\n"
						+ "Check with http://www.servicestack.net/ServiceStack.Hello/ to ensure you have configured ServiceStack properly.\n"
						+ "Otherwise you can explicitly set your httpHandler.Path by setting: EndpointHostConfig.ServiceStackPath");
				}
			}
			catch (Exception) {}
		}

		public ServiceManager ServiceManager { get; set; }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
		public string UsageExamplesBaseUri { get; set; }

		public string ServiceName { get; set; }
		public string DefaultContentType { get; set; }
		public bool AllowJsonpRequests { get; set; }
		public bool DebugMode { get; set; }
		public bool DebugOnlyReturnRequestInfo { get; set; }
		public string DebugAspNetHostEnvironment { get; set; }
		public string DebugHttpListenerHostEnvironment { get; set; }
		public List<string> DefaultDocuments { get; private set; }

		public HashSet<string> IgnoreFormatsInMetadata { get; set; }

		public HashSet<string> AllowFileExtensions { get; set; }

		public string ServiceStackHandlerFactoryPath { get; set; }
		public string DefaultRedirectPath { get; set; }
		public string MetadataRedirectPath { get; set; }
		public string NotFoundRedirectPath { get; set; }

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
