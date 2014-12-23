using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Linq;
using MarkdownSharp;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Markdown;
using ServiceStack.Metadata;
using ServiceStack.Text;

namespace ServiceStack
{
    public class HostConfig
    {
        public const string DefaultWsdlNamespace = "http://schemas.servicestack.net/types";
        public static string ServiceStackPath = null;

        private static HostConfig instance;
        public static HostConfig Instance
        {
            get { return instance ?? (instance = NewInstance()); }
        }

        public static HostConfig ResetInstance()
        {
            return instance = NewInstance();
        }

        public static HostConfig NewInstance()
        {
            var config = new HostConfig
            {
                WsdlServiceNamespace = DefaultWsdlNamespace,
                ApiVersion = "1.0",
                EmbeddedResourceSources = new List<Assembly>(),
                EmbeddedResourceBaseTypes = new[] { HostContext.AppHost.GetType(), typeof(Service) }.ToList(),
                EmbeddedResourceTreatAsFiles = new HashSet<string>(),
                LogFactory = new NullLogFactory(),
                EnableAccessRestrictions = true,
                WebHostPhysicalPath = "~".MapServerPath(),
                HandlerFactoryPath = ServiceStackPath,
                MetadataRedirectPath = null,
                DefaultContentType = null,
                PreferredContentTypes = new List<string> {
                    MimeTypes.Html, MimeTypes.Json, MimeTypes.Xml, MimeTypes.Jsv
                },
                AllowJsonpRequests = true,
                AllowRouteContentTypeExtensions = true,
                AllowNonHttpOnlyCookies = false,
                UseHttpsLinks = false,
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
                GlobalResponseHeaders = new Dictionary<string, string> {
                    { "Vary", "Accept" },
                    { "X-Powered-By", Env.ServerUserAgent },
                },
                IgnoreFormatsInMetadata = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                AllowFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "js", "ts", "jsx", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", "pdf",  
                    "jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", "svg",
                    "avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv", 
                    "flv", "swf", "xap", "xaml", "ogg", "mp4", "webm", "eot", "ttf", "woff", "woff2", "map"
                },
                DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
                DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
                EnableFeatures = Feature.All,
                WriteErrorsToResponse = true,
                ReturnsInnerException = true,
                MarkdownOptions = new MarkdownOptions(),
                MarkdownBaseType = typeof(MarkdownViewBase),
                MarkdownGlobalHelpers = new Dictionary<string, Type>(),
                HtmlReplaceTokens = new Dictionary<string, string>(),
                AddMaxAgeForStaticMimeTypes = new Dictionary<string, TimeSpan> {
		            { "image/gif", TimeSpan.FromHours(1) },
		            { "image/png", TimeSpan.FromHours(1) },
		            { "image/jpeg", TimeSpan.FromHours(1) },
	            },
                AppendUtf8CharsetOnContentTypes = new HashSet<string> { MimeTypes.Json, },
                RouteNamingConventions = new List<RouteNamingConventionDelegate> {
		            RouteNamingConvention.WithRequestDtoName,
		            RouteNamingConvention.WithMatchingAttributes,
		            RouteNamingConvention.WithMatchingPropertyNames
                },
                MapExceptionToStatusCode = new Dictionary<Type, int>(),
                OnlySendSessionCookiesSecurely = false,
                RestrictAllCookiesToDomain = null,
                DefaultJsonpCacheExpiration = new TimeSpan(0, 20, 0),
                MetadataVisibility = RequestAttributes.Any,
                Return204NoContentForEmptyResponse = true,
                AllowPartialResponses = true,
                AllowAclUrlReservation = true,
                AddRedirectParamsToQueryString = false,
                RedirectToDefaultDocuments = false,
                StripApplicationVirtualPath = false,
                ScanSkipPaths = new List<string> {
                    "/obj/", 
                    "/bin/",
                    "/node_modules/",
                    "/bower_components/",
                },
                IgnoreWarningsOnPropertyNames = new List<string> {
                    "format", "callback", "debug", "_", "authsecret", "Version", "version"
                },
                XmlWriterSettings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier:false),
                }
            };

            if (config.HandlerFactoryPath == null)
            {
                config.InferHttpHandlerPath();
            }

            return config;
        }

        public HostConfig()
        {
            if (instance == null) return;

            //Get a copy of the singleton already partially configured
            this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
            this.ApiVersion = instance.ApiVersion;
            this.EmbeddedResourceSources = instance.EmbeddedResourceSources;
            this.EmbeddedResourceBaseTypes = instance.EmbeddedResourceBaseTypes;
            this.EmbeddedResourceTreatAsFiles = instance.EmbeddedResourceTreatAsFiles;
            this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
            this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
            this.SoapServiceName = instance.SoapServiceName;
            this.XmlWriterSettings = instance.XmlWriterSettings;
            this.LogFactory = instance.LogFactory;
            this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
            this.WebHostUrl = instance.WebHostUrl;
            this.WebHostPhysicalPath = instance.WebHostPhysicalPath;
            this.DefaultRedirectPath = instance.DefaultRedirectPath;
            this.MetadataRedirectPath = instance.MetadataRedirectPath;
            this.HandlerFactoryPath = instance.HandlerFactoryPath;
            this.DefaultContentType = instance.DefaultContentType;
            this.PreferredContentTypes = instance.PreferredContentTypes;
            this.AllowJsonpRequests = instance.AllowJsonpRequests;
            this.AllowRouteContentTypeExtensions = instance.AllowRouteContentTypeExtensions;
            this.DebugMode = instance.DebugMode;
            this.DefaultDocuments = instance.DefaultDocuments;
            this.GlobalResponseHeaders = instance.GlobalResponseHeaders;
            this.IgnoreFormatsInMetadata = instance.IgnoreFormatsInMetadata;
            this.AllowFileExtensions = instance.AllowFileExtensions;
            this.EnableFeatures = instance.EnableFeatures;
            this.WriteErrorsToResponse = instance.WriteErrorsToResponse;
            this.ReturnsInnerException = instance.ReturnsInnerException;
            this.MarkdownOptions = instance.MarkdownOptions;
            this.MarkdownBaseType = instance.MarkdownBaseType;
            this.MarkdownGlobalHelpers = instance.MarkdownGlobalHelpers;
            this.HtmlReplaceTokens = instance.HtmlReplaceTokens;
            this.AddMaxAgeForStaticMimeTypes = instance.AddMaxAgeForStaticMimeTypes;
            this.AppendUtf8CharsetOnContentTypes = instance.AppendUtf8CharsetOnContentTypes;
            this.RouteNamingConventions = instance.RouteNamingConventions;
            this.MapExceptionToStatusCode = instance.MapExceptionToStatusCode;
            this.OnlySendSessionCookiesSecurely = instance.OnlySendSessionCookiesSecurely;
            this.RestrictAllCookiesToDomain = instance.RestrictAllCookiesToDomain;
            this.DefaultJsonpCacheExpiration = instance.DefaultJsonpCacheExpiration;
            this.MetadataVisibility = instance.MetadataVisibility;
            this.Return204NoContentForEmptyResponse = Return204NoContentForEmptyResponse;
            this.AllowNonHttpOnlyCookies = instance.AllowNonHttpOnlyCookies;
            this.AllowPartialResponses = instance.AllowPartialResponses;
            this.IgnoreWarningsOnPropertyNames = instance.IgnoreWarningsOnPropertyNames;
            this.FallbackRestPath = instance.FallbackRestPath;
            this.AllowAclUrlReservation = instance.AllowAclUrlReservation;
            this.AddRedirectParamsToQueryString = instance.AddRedirectParamsToQueryString;
            this.RedirectToDefaultDocuments = instance.RedirectToDefaultDocuments;
            this.StripApplicationVirtualPath = instance.StripApplicationVirtualPath;
            this.ScanSkipPaths = instance.ScanSkipPaths;
            this.AdminAuthSecret = instance.AdminAuthSecret;
        }

        public string WsdlServiceNamespace { get; set; }
        public string ApiVersion { get; set; }

        private RequestAttributes metadataVisibility;
        public RequestAttributes MetadataVisibility
        {
            get { return metadataVisibility; }
            set { metadataVisibility = value.ToAllowedFlagsSet(); }
        }

        public List<Type> EmbeddedResourceBaseTypes { get; set; }
        public List<Assembly> EmbeddedResourceSources { get; set; }
        public HashSet<string> EmbeddedResourceTreatAsFiles { get; set; }

        public string DefaultContentType { get; set; }
        public List<string> PreferredContentTypes { get; set; }
        internal string[] PreferredContentTypesArray = new string[0]; //use array at runtime
        public bool AllowJsonpRequests { get; set; }
        public bool AllowRouteContentTypeExtensions { get; set; }
        public bool DebugMode { get; set; }
        public string DebugAspNetHostEnvironment { get; set; }
        public string DebugHttpListenerHostEnvironment { get; set; }
        public List<string> DefaultDocuments { get; private set; }

        public List<string> IgnoreWarningsOnPropertyNames { get; private set; }

        public HashSet<string> IgnoreFormatsInMetadata { get; set; }

        public HashSet<string> AllowFileExtensions { get; set; }

        public string WebHostUrl { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public string HandlerFactoryPath { get; set; }
        public string DefaultRedirectPath { get; set; }
        public string MetadataRedirectPath { get; set; }

        public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
        public string SoapServiceName { get; set; }
        public XmlWriterSettings XmlWriterSettings { get; set; }
        public ILogFactory LogFactory { get; set; }
        public bool EnableAccessRestrictions { get; set; }
        public bool UseBclJsonSerializers { get; set; }
        public Dictionary<string, string> GlobalResponseHeaders { get; set; }
        public Feature EnableFeatures { get; set; }
        public bool ReturnsInnerException { get; set; }
        public bool WriteErrorsToResponse { get; set; }

        public MarkdownOptions MarkdownOptions { get; set; }
        public Type MarkdownBaseType { get; set; }
        public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }
        public Dictionary<string, string> HtmlReplaceTokens { get; set; }

        public HashSet<string> AppendUtf8CharsetOnContentTypes { get; set; }

        public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

        public List<RouteNamingConventionDelegate> RouteNamingConventions { get; set; }

        public Dictionary<Type, int> MapExceptionToStatusCode { get; set; }

        public bool OnlySendSessionCookiesSecurely { get; set; }
        public string RestrictAllCookiesToDomain { get; set; }

        public TimeSpan DefaultJsonpCacheExpiration { get; set; }
        public bool Return204NoContentForEmptyResponse { get; set; }
        public bool AllowPartialResponses { get; set; }
        public bool AllowNonHttpOnlyCookies { get; set; }
        public bool AllowAclUrlReservation { get; set; }
        public bool AddRedirectParamsToQueryString { get; set; }
        public bool RedirectToDefaultDocuments { get; set; }
        public bool StripApplicationVirtualPath { get; set; }

        //Skip scanning common VS.NET extensions
        public List<string> ScanSkipPaths { get; private set; }

        public bool UseHttpsLinks { get; set; }

        public string AdminAuthSecret { get; set; }

        public FallbackRestPathDelegate FallbackRestPath { get; set; }

        const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";
        private HashSet<string> razorNamespaces;
        public HashSet<string> RazorNamespaces
        {
            get
            {
                if (razorNamespaces != null)
                    return razorNamespaces;

                razorNamespaces = new HashSet<string>();
                //Infer from <system.web.webPages.razor> - what VS.NET's intell-sense uses
                var configPath = HostContext.GetAppConfigPath();
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

                //log.Debug("Loaded Razor Namespaces: in {0}: {1}: {2}"
                //    .Fmt(configPath, "~/Web.config".MapHostAbsolutePath(), razorNamespaces.Dump()));

                return razorNamespaces;
            }
        }

        private System.Configuration.Configuration GetAppConfig()
        {
            Assembly entryAssembly;

            //Read the user-defined path in the Web.Config
            if (HostContext.IsAspNetHost)
                return WebConfigurationManager.OpenWebConfiguration("~/");

            if ((entryAssembly = Assembly.GetEntryAssembly()) != null)
                return ConfigurationManager.OpenExeConfiguration(entryAssembly.Location);

            return null;
        }

        private void InferHttpHandlerPath()
        {
            try
            {
                var config = GetAppConfig();
                if (config == null) return;

                SetPathsFromConfiguration(config, null);

                if (MetadataRedirectPath == null)
                {
                    foreach (ConfigurationLocation location in config.Locations)
                    {
                        SetPathsFromConfiguration(location.OpenConfiguration(), (location.Path ?? "").ToLower());

                        if (MetadataRedirectPath != null) { break; }
                    }
                }

                if (HostContext.IsAspNetHost && MetadataRedirectPath == null)
                {
                    throw new ConfigurationErrorsException(
                        "Unable to infer ServiceStack's <httpHandler.Path/> from the Web.Config\n"
                        + "Check with https://github.com/ServiceStack/ServiceStack/wiki/Create-your-first-webservice to ensure you have configured ServiceStack properly.\n"
                        + "Otherwise you can explicitly set your httpHandler.Path by setting: EndpointHostConfig.ServiceStackPath");
                }
            }
            catch (Exception) { }
        }

        private void SetPathsFromConfiguration(System.Configuration.Configuration config, string locationPath)
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
            var pathsNotSet = MetadataRedirectPath == null;
            if (pathsNotSet)
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

                //In some MVC Hosts auto-inferencing doesn't work, in these cases assume the most likely default of "/api" path
                pathsNotSet = MetadataRedirectPath == null;
                if (pathsNotSet)
                {
                    var isMvcHost = Type.GetType("System.Web.Mvc.Controller") != null;
                    if (isMvcHost)
                    {
                        SetPaths("api", null);
                    }
                }
            }
        }

        private void SetPaths(string handlerPath, string locationPath)
        {
            if (handlerPath == null) return;

            if (locationPath == null)
            {
                handlerPath = handlerPath.Replace("*", String.Empty);
            }

            HandlerFactoryPath = locationPath ??
                (String.IsNullOrEmpty(handlerPath) ? null : handlerPath);

            MetadataRedirectPath = "metadata";
        }

        private static string ExtractHandlerPathFromWebServerConfigurationXml(string rawXml)
        {
            return XDocument.Parse(rawXml).Root.Element("handlers")
                .Descendants("add")
                .Where(handler => EnsureHandlerTypeAttribute(handler).StartsWith("ServiceStack"))
                .Select(handler => handler.Attribute("path").Value)
                .FirstOrDefault();
        }

        private static string EnsureHandlerTypeAttribute(XElement handler)
        {
            if (handler.Attribute("type") != null && !String.IsNullOrEmpty(handler.Attribute("type").Value))
            {
                return handler.Attribute("type").Value;
            }
            return String.Empty;
        }
    }

}
