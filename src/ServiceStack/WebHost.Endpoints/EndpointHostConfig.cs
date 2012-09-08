using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Configuration;
using System.Xml.Linq;
using MarkdownSharp;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel;
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
							"default.md",
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
						HtmlReplaceTokens = new Dictionary<string, string>(),
						AddMaxAgeForStaticMimeTypes = new Dictionary<string, TimeSpan> {
							{ "image/gif", TimeSpan.FromHours(1) },
							{ "image/png", TimeSpan.FromHours(1) },
							{ "image/jpeg", TimeSpan.FromHours(1) },
						},
						AppendUtf8CharsetOnContentTypes = new HashSet<string> { ContentType.Json, },
						RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>>(),
						CustomHttpHandlers = new Dictionary<HttpStatusCode, IHttpHandler>(),
						DefaultJsonpCacheExpiration = new TimeSpan(0, 20, 0)
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
			this.HtmlReplaceTokens = instance.HtmlReplaceTokens;
			this.AddMaxAgeForStaticMimeTypes = instance.AddMaxAgeForStaticMimeTypes;
			this.AppendUtf8CharsetOnContentTypes = instance.AppendUtf8CharsetOnContentTypes;
			this.RawHttpHandlers = instance.RawHttpHandlers;
			this.CustomHttpHandlers = instance.CustomHttpHandlers;
			this.DefaultJsonpCacheExpiration = instance.DefaultJsonpCacheExpiration;
		}

        public static string GetAppConfigPath()
        {
            if (EndpointHost.AppHost == null) return null;

            var configPath = "~/web.config".MapHostAbsolutePath();
            if (File.Exists(configPath))
                return configPath;

            var appHostDll = new FileInfo(EndpointHost.AppHost.GetType().Assembly.Location).Name;
            configPath = "~/{0}.config".Fmt(appHostDll).MapAbsolutePath();
            return File.Exists(configPath) ? configPath : null;
        }

	    const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";
	    private static HashSet<string> razorNamespaces;
	    public static HashSet<string> RazorNamespaces
        {
            get
            {
                if (razorNamespaces != null)
                    return razorNamespaces;

                razorNamespaces = new HashSet<string>();
                //Infer from <system.web.webPages.razor> - what VS.NET's intell-sense uses
                var configPath = GetAppConfigPath();
                if (configPath != null)
                {
                    var xml = configPath.ReadAllText();
                    var doc = XElement.Parse(xml);
                    doc.AnyElement("system.web.webPages.razor")
                        .AnyElement("pages")
                            .AnyElement("namespaces")
                                .AllElements("add").ToList()
                                    .ForEach(x => razorNamespaces.Add(x.AnyAttribute("namespace").Value));
                }

                //E.g. <add key="servicestack.razor.namespaces" value="System,ServiceStack.Text" />
                if (ConfigUtils.GetNullableAppSetting(NamespacesAppSettingsKey) != null)
                {
                    ConfigUtils.GetListFromAppSetting(NamespacesAppSettingsKey)
                        .ForEach(x => razorNamespaces.Add(x));
                }

                return razorNamespaces;
            }
        }

	    private static System.Configuration.Configuration GetAppConfig()
        {
            Assembly entryAssembly;
            
            //Read the user-defined path in the Web.Config
            if (EndpointHost.AppHost is AppHostBase)
                return WebConfigurationManager.OpenWebConfiguration("~/");
            
            if ((entryAssembly = Assembly.GetEntryAssembly()) != null)
                return ConfigurationManager.OpenExeConfiguration(entryAssembly.Location);
            
            return null;
        }

		private static void InferHttpHandlerPath()
		{
			try
			{
				var config = GetAppConfig();
                if (config == null) return;

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
			if (config == null)
				return;

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
					if (!String.IsNullOrEmpty(rawXml))
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
				handlerPath = handlerPath.Replace("*", String.Empty);
			}

			instance.ServiceStackHandlerFactoryPath = locationPath ??
				(String.IsNullOrEmpty(handlerPath) ? null : handlerPath);

			instance.MetadataRedirectPath = PathUtils.CombinePaths(
				null != locationPath ? instance.ServiceStackHandlerFactoryPath : handlerPath
				, "metadata");
		}

		private static string ExtractHandlerPathFromWebServerConfigurationXml(string rawXml)
		{
			return XDocument.Parse(rawXml).Root.Element("handlers")
				.Descendants("add")
				.Where(handler => (handler.Attribute("type").Value ?? String.Empty).StartsWith("ServiceStack"))
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
		public Dictionary<string, string> HtmlReplaceTokens { get; set; }

		public HashSet<string> AppendUtf8CharsetOnContentTypes { get; set; }

		public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

		public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; set; }

		public Dictionary<HttpStatusCode, IHttpHandler> CustomHttpHandlers { get; set; }

		public TimeSpan DefaultJsonpCacheExpiration { get; set; }

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
			if (!String.IsNullOrEmpty(this.defaultOperationNamespace)
				|| this.ServiceController == null) return null;

			foreach (var operationType in this.ServiceController.OperationTypes)
			{
				var attrs = operationType.GetCustomAttributes(
					typeof(DataContractAttribute), false);

				if (attrs.Length <= 0) continue;

				var attr = (DataContractAttribute)attrs[0];

				if (String.IsNullOrEmpty(attr.Namespace)) continue;

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
					String.Format("'{0}' Features have been disabled by your administrator", usesFeatures));
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
