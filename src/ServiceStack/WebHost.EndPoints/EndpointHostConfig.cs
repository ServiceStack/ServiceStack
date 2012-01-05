using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Configuration;
using System.Xml.Linq;
using MarkdownSharp;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Markdown;
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
					instance = new EndpointHostConfig {
						MetadataPageBodyHtml = @"
    <br />
    <h3>Client Usage Examples:</h3>
    <ul>
        <li><a href=""{0}/UsingServiceClients.cs"">Using Service Clients</a></li>
        <li><a href=""{0}/UsingDtoFromAssembly.cs"">Using Dto From Assembly</a></li>
        <li><a href=""{0}/UsingDtoFromXsd.cs"">Using Dto From Xsd</a></li>
        <li><a href=""{0}/UsingServiceReferenceClient.cs"">Using Service Reference Client</a></li>
        <li><a href=""{0}/UsingSvcutilGeneratedClient.cs"">Using SvcUtil Generated Client</a></li>
        <li><a href=""{0}/UsingRawHttpClient.cs"">Using Raw Http Client</a></li>
        <li><a href=""{0}/UsingRestAndJson.cs"">Using Rest and Json</a></li>
        <li><a href=""{0}/UsingRestAndXml.cs"">Using Rest and Xml</a></li>
    </ul>".Fmt(DefaultUsageExamplesBaseUri),
						MetadataOperationPageBodyHtml = @"
    <br />
    <h3>Usage Examples:</h3>
    <ul>
        <li><a href=""{0}/UsingRestAndJson.cs"">Using Rest and JSON</a></li>
        <li><a href=""{0}/UsingRestAndXml.cs"">Using Rest and XML</a></li>
    </ul>".Fmt(DefaultUsageExamplesBaseUri),
						LogFactory = new NullLogFactory(),
						EnableAccessRestrictions = true,
						WsdlServiceNamespace = "http://schemas.servicestack.net/types",
						WebHostPhysicalPath = "~".MapServerPath(),
						ServiceStackHandlerFactoryPath = ServiceStackPath,
						MetadataRedirectPath = null,
						DefaultContentType = null,
						AllowJsonpRequests = true,
						DebugMode = false,
						DefaultDocuments = new List<string> {
							"default.htm",
							"default.html",
							"default.cshtml",
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
							"flv", "xap", "xaml", 
						},
						DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
						DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
						EnableFeatures = Feature.All,
						WriteErrorsToResponse = true,
						MarkdownOptions = new MarkdownOptions(),
						MarkdownBaseType = typeof(MarkdownViewBase),
						MarkdownGlobalHelpers = new Dictionary<string, Type>(),
						MarkdownSearchPath = "~".MapServerPath(),
						MarkdownReplaceTokens = new Dictionary<string, string>(),
						RazorSearchPath = "~".MapServerPath(),
						AddMaxAgeForStaticMimeTypes = new Dictionary<string, TimeSpan> {
							{ "image/gif", TimeSpan.FromHours(1) },
							{ "image/png", TimeSpan.FromHours(1) },
							{ "image/jpeg", TimeSpan.FromHours(1) },
						},
						RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>>(),
						CustomHttpHandlers = new Dictionary<HttpStatusCode, IHttpHandler>(),
					};

					if (instance.ServiceStackHandlerFactoryPath == null)
					{
						InferHttpHandlerPath();
					}
				}
				return instance;
			}
		}

		public EndpointHostConfig(string serviceName, ServiceManager serviceManager)
			: this()
		{
			this.ServiceName = serviceName;
			this.ServiceManager = serviceManager;
		}

		public EndpointHostConfig()
		{
			if (instance == null) return;

			//Get a copy of the singleton already partially configured
			this.MetadataPageBodyHtml = instance.MetadataPageBodyHtml;
			this.MetadataOperationPageBodyHtml = instance.MetadataOperationPageBodyHtml;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
			this.LogFactory = instance.LogFactory;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
			this.WebHostUrl = instance.WebHostUrl;
			this.WebHostPhysicalPath = instance.WebHostPhysicalPath;
			this.DefaultRedirectPath = instance.DefaultRedirectPath;
			this.MetadataRedirectPath = instance.MetadataRedirectPath;
			this.ServiceStackHandlerFactoryPath = instance.ServiceStackHandlerFactoryPath;
			this.DefaultContentType = instance.DefaultContentType;
			this.AllowJsonpRequests = instance.AllowJsonpRequests;
			this.DebugMode = instance.DebugMode;
			this.DefaultDocuments = instance.DefaultDocuments;
			this.GlobalResponseHeaders = instance.GlobalResponseHeaders;
			this.IgnoreFormatsInMetadata = instance.IgnoreFormatsInMetadata;
			this.AllowFileExtensions = instance.AllowFileExtensions;
			this.EnableFeatures = instance.EnableFeatures;
			this.WriteErrorsToResponse = instance.WriteErrorsToResponse;
			this.MarkdownOptions = instance.MarkdownOptions;
			this.MarkdownBaseType = instance.MarkdownBaseType;
			this.MarkdownGlobalHelpers = instance.MarkdownGlobalHelpers;
			this.MarkdownSearchPath = instance.MarkdownSearchPath;
			this.MarkdownReplaceTokens = instance.MarkdownReplaceTokens;
			this.RazorSearchPath = instance.RazorSearchPath;
			this.RazorBaseType = instance.RazorBaseType;
			this.AddMaxAgeForStaticMimeTypes = instance.AddMaxAgeForStaticMimeTypes;
			this.RawHttpHandlers = instance.RawHttpHandlers;
			this.CustomHttpHandlers = instance.CustomHttpHandlers;
		}

		private static void InferHttpHandlerPath()
		{
			try
			{
				//Read the user-defined path in the Web.Config
				var config = WebConfigurationManager.OpenWebConfiguration("~/");
				SetPathsFromConfiguration(config, null);

				if (instance.MetadataRedirectPath == null)
				{
					foreach (ConfigurationLocation location in config.Locations)
					{
						SetPathsFromConfiguration(location.OpenConfiguration(), (location.Path ?? "").ToLower());

						if (instance.MetadataRedirectPath != null) { break; }
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
			catch (Exception) { }
		}

		private static void SetPathsFromConfiguration(System.Configuration.Configuration config, string locationPath)
		{
			//standard config
			var handlersSection = config.GetSection("system.web/httpHandlers") as HttpHandlersSection;
			if (handlersSection != null)
			{
				for (var i = 0; i < handlersSection.Handlers.Count; i++)
				{
					var httpHandler = handlersSection.Handlers[i];
					if (!httpHandler.Type.StartsWith("ServiceStack"))
						continue;

					SetPaths(httpHandler.Path, locationPath);
					break;
				}
			}

			//IIS7+ integrated mode system.webServer/handlers
			if (instance.MetadataRedirectPath == null)
			{
				var webServerSection = config.GetSection("system.webServer");
				if (webServerSection != null)
				{
					var rawXml = webServerSection.SectionInformation.GetRawXml();
					if (!string.IsNullOrEmpty(rawXml))
					{
						SetPaths(ExtractHandlerPathFromWebServerConfigurationXml(rawXml), locationPath);
					}
				}
			}
		}

		private static void SetPaths(string handlerPath, string locationPath)
		{
			if (null == handlerPath) { return; }

			if (null == locationPath)
			{
				handlerPath = handlerPath.Replace("*", string.Empty);
			}

			instance.ServiceStackHandlerFactoryPath = locationPath ??
				(string.IsNullOrEmpty(handlerPath) ? null : handlerPath);

			instance.MetadataRedirectPath = PathUtils.CombinePaths(
				null != locationPath ? instance.ServiceStackHandlerFactoryPath : handlerPath
				, "metadata");
		}

		private static string ExtractHandlerPathFromWebServerConfigurationXml(string rawXml)
		{
			return XDocument.Parse(rawXml).Root.Element("handlers")
				.Descendants("add")
				.Where(handler => (handler.Attribute("type").Value
				?? string.Empty).StartsWith("ServiceStack"))
				.Select(handler => handler.Attribute("path").Value)
				.FirstOrDefault();
		}

		public ServiceManager ServiceManager { get; internal set; }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }
		public string MetadataPageBodyHtml { get; set; }
		public string MetadataOperationPageBodyHtml { get; set; }

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

		public string WebHostUrl { get; set; }
		public string WebHostPhysicalPath { get; set; }
		public string ServiceStackHandlerFactoryPath { get; set; }
		public string DefaultRedirectPath { get; set; }
		public string MetadataRedirectPath { get; set; }

		public string WsdlServiceNamespace { get; set; }
		public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
		public ILogFactory LogFactory { get; set; }
		public bool EnableAccessRestrictions { get; set; }
		public bool UseBclJsonSerializers { get; set; }
		public Dictionary<string, string> GlobalResponseHeaders { get; set; }
		public Feature EnableFeatures { get; set; }
		public bool WriteErrorsToResponse { get; set; }

		public MarkdownOptions MarkdownOptions { get; set; }
		public Type MarkdownBaseType { get; set; }
		public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }
		public string MarkdownSearchPath { get; set; }
		public Dictionary<string, string> MarkdownReplaceTokens { get; set; }

		public string RazorSearchPath { get; set; }
		public Type RazorBaseType { get; set; }

		public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

		public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; set; }

		public Dictionary<HttpStatusCode, IHttpHandler> CustomHttpHandlers { get; set; }

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

		public bool HasFeature(Feature feature)
		{
			return (feature & EndpointHost.Config.EnableFeatures) == feature;
		}

		public void AssertFeatures(Feature usesFeatures)
		{
			if (EndpointHost.Config.EnableFeatures == Feature.All) return;

			if (!HasFeature(usesFeatures))
			{
				throw new NotSupportedException(
					string.Format("'{0}' Features have been disabled by your administrator", usesFeatures));
			}
		}

		public void AssertContentType(string contentType)
		{
			if (EndpointHost.Config.EnableFeatures == Feature.All) return;

			var contentTypeFeature = ContentType.GetFeature(contentType);
			AssertFeatures(contentTypeFeature);
		}
	}

}
