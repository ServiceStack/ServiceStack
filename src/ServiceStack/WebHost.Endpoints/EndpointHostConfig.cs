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
		//use of "_" for private member variables makes it easier to maintain thread safe code, by clearly seperating stack vars from heap vars

	    private readonly static ILog _log = LogManager.GetLogger(typeof (EndpointHostConfig));

		public static bool SkipPathValidation = false;
		public static string ServiceStackPath = null;

		private const string DefaultUsageExamplesBaseUri =
			"https://github.com/ServiceStack/ServiceStack.Extras/blob/master/doc/UsageExamples";

		private static Dictionary<string, EndpointHostConfig> _namedConfigs = new Dictionary<string, EndpointHostConfig>();
		private readonly static object _syncRoot = new object();

		/// <summary>
		/// Gets a <see cref="EndpointHostConfig"/> by name, and creates a new one if one doesn't exist by that name.
		/// </summary>
		/// <param name="name">The name of the config to return or create.</param>
		/// <returns>Returns the instance.</returns>
		/// <remarks>This method is thread safe.</remarks>
		internal static EndpointHostConfig GetNamedConfig(string name)
		{
			EndpointHostConfig config;
			if (_namedConfigs.TryGetValue(name, out config))
			{
				return config;
			}

			lock (_syncRoot) 
			{
				if (_namedConfigs.TryGetValue(name, out config)) //double checked locking works in .Net unlike Java
				{
					return config;
				}

				config = CreateDefaultConfig();
				//Question: do we use InferHttpHandlerPath here? or some analagous setup here? 

				var namedConfigs = new Dictionary<string, EndpointHostConfig>(_namedConfigs);
				namedConfigs.Add(name, config);
				_namedConfigs = namedConfigs;
				return config;
			}
		}

		internal static void RemoveNamedConfig(string name)
		{
			if (string.IsNullOrEmpty(name)) return;

			lock(_syncRoot)
			{
				var namedConfigs = new Dictionary<string, EndpointHostConfig>(_namedConfigs);
				namedConfigs.Remove(name);
				_namedConfigs = namedConfigs;
			}
		}

		private static EndpointHostConfig _instance;
		/// <summary>
		/// Gets the default instance of the config.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		public static EndpointHostConfig Instance
		{
			get
			{
				if (_instance != null) return _instance;

				lock (_syncRoot)
				{
					if (_instance != null) return _instance;
				
					var instance = CreateDefaultConfig();

					if (instance.ServiceStackHandlerFactoryPath == null)
					{
						InferHttpHandlerPath(instance);
					}
					_instance = instance;
				}
				return _instance;
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
			EndpointHostConfig instance = _instance;
			//copy the default singleton already partially configured
			ExtractExistingValues(instance);
		}

		private void ExtractExistingValues(EndpointHostConfig existing)
		{
			if (existing == null) return;
			this.MetadataPageBodyHtml = existing.MetadataPageBodyHtml;
			this.MetadataOperationPageBodyHtml = existing.MetadataOperationPageBodyHtml;
			this.EnableAccessRestrictions = existing.EnableAccessRestrictions;
			this.ServiceEndpointsMetadataConfig = existing.ServiceEndpointsMetadataConfig;
			this.LogFactory = existing.LogFactory;
			this.EnableAccessRestrictions = existing.EnableAccessRestrictions;
			this.WsdlServiceNamespace = existing.WsdlServiceNamespace;
			this.WebHostUrl = existing.WebHostUrl;
			this.WebHostPhysicalPath = existing.WebHostPhysicalPath;
			this.DefaultRedirectPath = existing.DefaultRedirectPath;
			this.MetadataRedirectPath = existing.MetadataRedirectPath;
			this.ServiceStackHandlerFactoryPath = existing.ServiceStackHandlerFactoryPath;
			this.DefaultContentType = existing.DefaultContentType;
			this.AllowJsonpRequests = existing.AllowJsonpRequests;
			this.DebugMode = existing.DebugMode;
			this.DefaultDocuments = existing.DefaultDocuments;
			this.GlobalResponseHeaders = existing.GlobalResponseHeaders;
			this.IgnoreFormatsInMetadata = existing.IgnoreFormatsInMetadata;
			this.AllowFileExtensions = existing.AllowFileExtensions;
			this.EnableFeatures = existing.EnableFeatures;
			this.WriteErrorsToResponse = existing.WriteErrorsToResponse;
			this.MarkdownOptions = existing.MarkdownOptions;
			this.MarkdownBaseType = existing.MarkdownBaseType;
			this.MarkdownGlobalHelpers = existing.MarkdownGlobalHelpers;
			this.HtmlReplaceTokens = existing.HtmlReplaceTokens;
			this.AddMaxAgeForStaticMimeTypes = existing.AddMaxAgeForStaticMimeTypes;
			this.AppendUtf8CharsetOnContentTypes = existing.AppendUtf8CharsetOnContentTypes;
			this.RawHttpHandlers = existing.RawHttpHandlers;
			this.CustomHttpHandlers = existing.CustomHttpHandlers;
			this.DefaultJsonpCacheExpiration = existing.DefaultJsonpCacheExpiration;
		}

		private static EndpointHostConfig CreateDefaultConfig()
		{
			return new EndpointHostConfig
			{
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
		}

        public static string GetAppConfigPath()
        {
            var configPath = "~/web.config".MapHostAbsolutePath();
            if (File.Exists(configPath))
                return configPath;
            
            configPath = "~/Web.config".MapHostAbsolutePath(); //*nix FS FTW!
            if (File.Exists(configPath))
                return configPath;

            var appHostDll = new FileInfo(EndpointHost.AppHost.GetType().Assembly.Location).Name;
            configPath = "~/{0}.config".Fmt(appHostDll).MapAbsolutePath();
            return File.Exists(configPath) ? configPath : null;
        }

	    const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";
	    private static HashSet<string> _razorNamespaces;
		/// <summary>
		/// Gets the Razor Namespaces.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
	    public static HashSet<string> RazorNamespaces
        {
            get
            {
                if (_razorNamespaces != null) return _razorNamespaces;

				lock (_syncRoot)
				{
					if (_razorNamespaces != null) return _razorNamespaces; //double checked locking works fine in .Net, not in Java

					var razorNamespaces = new HashSet<string>();
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
                
					_log.Debug("Loaded Razor Namespaces: in {0}: {1}: {2}"
                    .Fmt(configPath, "~/Web.config".MapHostAbsolutePath(), razorNamespaces.Dump()));
					
					//publish value last
					_razorNamespaces = razorNamespaces;
				}

                return _razorNamespaces;
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

		private static void InferHttpHandlerPath(EndpointHostConfig instance)
		{
			try
			{
				var config = GetAppConfig();
                if (config == null) return;

				SetPathsFromConfiguration(instance, config, null);

				if (instance.MetadataRedirectPath == null)
				{
					foreach (ConfigurationLocation location in config.Locations)
					{
						SetPathsFromConfiguration(instance, location.OpenConfiguration(), (location.Path ?? "").ToLower());

						if (instance.MetadataRedirectPath != null) { break; }
					}
				}
			}
			catch (Exception exc)
			{
				_log.Error("Bad Config", exc);
			}
			
			if (!SkipPathValidation && instance.MetadataRedirectPath == null)
			{
				throw new ConfigurationErrorsException(
					"Unable to infer ServiceStack's <httpHandler.Path/> from the Web.Config\n"
					+ "Check with http://www.servicestack.net/ServiceStack.Hello/ to ensure you have configured ServiceStack properly.\n"
					+ "Otherwise you can explicitly set your httpHandler.Path by setting: EndpointHostConfig.ServiceStackPath");
			}
		}

		private static void SetPathsFromConfiguration(EndpointHostConfig instance, System.Configuration.Configuration config, string locationPath)
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

					SetPaths(instance, httpHandler.Path, locationPath);
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
						SetPaths(instance, ExtractHandlerPathFromWebServerConfigurationXml(rawXml), locationPath);
					}
				}
			}
		}

		private static void SetPaths(EndpointHostConfig instance, string handlerPath, string locationPath)
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

		private string _defaultOperationNamespace;
		/// <summary>
		/// Gets the default operation namespace.
		/// </summary>
		/// <remarks>This method is thread safe</remarks>
		public string DefaultOperationNamespace
		{
			get
			{
				if (_defaultOperationNamespace != null) return _defaultOperationNamespace;
			
				string defaultNamespace = GetDefaultNamespace();
				if (defaultNamespace == null) return "";

				lock (_syncRoot)
				{
					if (_defaultOperationNamespace == null)
					{
						_defaultOperationNamespace = defaultNamespace;
					}
				}
					return _defaultOperationNamespace;
			}
			set
			{
				_defaultOperationNamespace = value;
			}
		}

		private string GetDefaultNamespace()
		{
			if (this.ServiceController == null) return null;

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
