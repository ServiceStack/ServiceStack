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
using ServiceStack.Common.ServiceModel;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
    public class EndpointHostConfig
    {
        private static ILog log = LogManager.GetLogger(typeof(EndpointHostConfig));

        public static readonly string PublicKey = "<RSAKeyValue><Modulus>xRzMrP3m+3kvT6239OP1YuWIfc/S7qF5NJiPe2/kXnetXiuYtSL4bQRIX1qYh4Cz+dXqZE/sNGJJ4jl2iJQa1tjp+rK28EG6gcuTDHJdvOBBF+aSwJy1MSiT8D0KtP6pe2uvjl9m3jZP/8uRePZTSkt/GjlPOk85JXzOsyzemlaLFiJoGImGvp8dw8vQ7jzA3Ynmywpt5OQxklJfrfALHJ93ny1M5lN5Q+bGPEHLXNCXfF05EA0l9mZpa4ouicYvlbY/OAwefFXIwPQN9ER6Pu7Eq9XWLvnh1YUH8HDckuKK+ESWbAuOgnVbUDEF1BreoWutJ//a/oLDR87Q36cmwQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        public static readonly string LicensePublicKey = "<RSAKeyValue><Modulus>19kx2dJoOIrMYypMTf8ssiCALJ7RS/Iz2QG0rJtYJ2X0+GI+NrgOCapkh/9aDVBieobdClnuBgW08C5QkfBdLRqsptiSu50YIqzVaNBMwZPT0e7Ke02L/fV/M/fVPsolHwzMstKhdWGdK8eNLF4SsLEcvnb79cx3/GnZbXku/ro5eOrTseKL3s4nM4SdMRNn7rEAU0o0Ijb3/RQbhab8IIRB4pHwk1mB+j/mcAQAtMerwpHfwpEBLWlQyVpu0kyKJCEkQjbaPzvfglDRpyBOT5GMUnrcTT/sBr5kSJYpYrgHnA5n4xJnvrnyFqdzXwgGFlikRTbc60pk1cQEWcHgYw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static bool SkipPathValidation = false;
        /// <summary>
        /// Use: \[Route\("[^\/]  regular expression to find violating routes in your sln
        /// </summary>
        public static bool SkipRouteValidation = false;

        public static string ServiceStackPath = null;

        private static EndpointHostConfig instance;
        public static EndpointHostConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EndpointHostConfig
                    {
                        MetadataTypesConfig = new MetadataTypesConfig(addDefaultXmlNamespace: "http://schemas.servicestack.net/types"),
                        WsdlServiceNamespace = "http://schemas.servicestack.net/types",
                        WsdlSoapActionNamespace = "http://schemas.servicestack.net/types",
                        MetadataPageBodyHtml = @"<br />
                            <h3><a href=""https://github.com/ServiceStack/ServiceStack/wiki/Clients-overview"">Clients Overview</a></h3>",
                        MetadataOperationPageBodyHtml = @"<br />
                            <h3><a href=""https://github.com/ServiceStack/ServiceStack/wiki/Clients-overview"">Clients Overview</a></h3>",
                        MetadataCustomPath = "Views/Templates/Metadata/",
                        UseCustomMetadataTemplates = false,

                        LogFactory = new NullLogFactory(),
                        EnableAccessRestrictions = true,
                        WebHostPhysicalPath = "~".MapServerPath(),
                        ServiceStackHandlerFactoryPath = ServiceStackPath,
                        MetadataRedirectPath = null,
                        DefaultContentType = null,
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
                        GlobalResponseHeaders = new Dictionary<string, string> { { "X-Powered-By", Env.ServerUserAgent } },
                        IgnoreFormatsInMetadata = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase),
                        AllowFileExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
						{
							"js", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", "pdf",  
							"jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", "svg",
							"avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv", 
							"flv", "xap", "xaml", "ogg", "mp4", "webm", "eot", "ttf", "woff"
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
                        AppendUtf8CharsetOnContentTypes = new HashSet<string> { ContentType.Json, },
                        RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>>(),
                        RouteNamingConventions = new List<RouteNamingConventionDelegate> {
					        RouteNamingConvention.WithRequestDtoName,
					        RouteNamingConvention.WithMatchingAttributes,
					        RouteNamingConvention.WithMatchingPropertyNames
                        },
                        CustomHttpHandlers = new Dictionary<HttpStatusCode, IServiceStackHttpHandler>(),
                        GlobalHtmlErrorHttpHandler = null,
                        MapExceptionToStatusCode = new Dictionary<Type, int>(),
                        OnlySendSessionCookiesSecurely = false,
                        RestrictAllCookiesToDomain = null,
                        DefaultJsonpCacheExpiration = new TimeSpan(0, 20, 0),
                        MetadataVisibility = EndpointAttributes.Any,
                        Return204NoContentForEmptyResponse = true,
                        AllowPartialResponses = true,
                        AllowAclUrlReservation = true,
                        IgnoreWarningsOnPropertyNames = new List<string> {
                            "format", "callback", "debug", "_", "authsecret"
                        }
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
            this.MetadataTypesConfig = instance.MetadataTypesConfig;
            this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
            this.WsdlSoapActionNamespace = instance.WsdlSoapActionNamespace;
            this.MetadataPageBodyHtml = instance.MetadataPageBodyHtml;
            this.MetadataOperationPageBodyHtml = instance.MetadataOperationPageBodyHtml;
            this.MetadataCustomPath = instance.MetadataCustomPath;
            this.UseCustomMetadataTemplates = instance.UseCustomMetadataTemplates;
            this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
            this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
            this.LogFactory = instance.LogFactory;
            this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
            this.WebHostUrl = instance.WebHostUrl;
            this.WebHostPhysicalPath = instance.WebHostPhysicalPath;
            this.DefaultRedirectPath = instance.DefaultRedirectPath;
            this.MetadataRedirectPath = instance.MetadataRedirectPath;
            this.ServiceStackHandlerFactoryPath = instance.ServiceStackHandlerFactoryPath;
            this.DefaultContentType = instance.DefaultContentType;
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
            this.RawHttpHandlers = instance.RawHttpHandlers;
            this.RouteNamingConventions = instance.RouteNamingConventions;
            this.CustomHttpHandlers = instance.CustomHttpHandlers;
            this.GlobalHtmlErrorHttpHandler = instance.GlobalHtmlErrorHttpHandler;
            this.MapExceptionToStatusCode = instance.MapExceptionToStatusCode;
            this.OnlySendSessionCookiesSecurely = instance.OnlySendSessionCookiesSecurely;
            this.RestrictAllCookiesToDomain = instance.RestrictAllCookiesToDomain;
            this.DefaultJsonpCacheExpiration = instance.DefaultJsonpCacheExpiration;
            this.MetadataVisibility = instance.MetadataVisibility;
            this.Return204NoContentForEmptyResponse = Return204NoContentForEmptyResponse;
            this.AllowNonHttpOnlyCookies = instance.AllowNonHttpOnlyCookies;
            this.AllowPartialResponses = instance.AllowPartialResponses;
            this.IgnoreWarningsOnPropertyNames = instance.IgnoreWarningsOnPropertyNames;
            this.PreExecuteServiceFilter = instance.PreExecuteServiceFilter;
            this.PostExecuteServiceFilter = instance.PostExecuteServiceFilter;
            this.FallbackRestPath = instance.FallbackRestPath;
            this.AllowAclUrlReservation = instance.AllowAclUrlReservation;
            this.AdminAuthSecret = instance.AdminAuthSecret;
        }

        public static string GetAppConfigPath()
        {
            if (EndpointHost.AppHost == null) return null;

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

                //log.Debug("Loaded Razor Namespaces: in {0}: {1}: {2}"
                //    .Fmt(configPath, "~/Web.config".MapHostAbsolutePath(), razorNamespaces.Dump()));

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
            var pathsNotSet = instance.MetadataRedirectPath == null;
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
                pathsNotSet = instance.MetadataRedirectPath == null;
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

        private static void SetPaths(string handlerPath, string locationPath)
        {
            if (handlerPath == null) return;

            if (locationPath == null)
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

        public ServiceManager ServiceManager { get; internal set; }
        public ServiceMetadata Metadata { get { return ServiceManager.Metadata; } }
        public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }

        public MetadataTypesConfig MetadataTypesConfig { get; set; }
        public string WsdlServiceNamespace { get; set; }
        public string WsdlSoapActionNamespace { get; set; }

        private EndpointAttributes metadataVisibility;
        public EndpointAttributes MetadataVisibility
        {
            get { return metadataVisibility; }
            set { metadataVisibility = value.ToAllowedFlagsSet(); }
        }

        public string MetadataPageBodyHtml { get; set; }
        public string MetadataOperationPageBodyHtml { get; set; }
        public string MetadataCustomPath { get; set; }
        public bool UseCustomMetadataTemplates { get; set; }

        public string ServiceName { get; set; }
        public string SoapServiceName { get; set; }
        public string DefaultContentType { get; set; }
        public bool AllowJsonpRequests { get; set; }
        public bool AllowRouteContentTypeExtensions { get; set; }
        public bool DebugMode { get; set; }
        public bool DebugOnlyReturnRequestInfo { get; set; }
        public string DebugAspNetHostEnvironment { get; set; }
        public string DebugHttpListenerHostEnvironment { get; set; }
        public List<string> DefaultDocuments { get; private set; }

        public List<string> IgnoreWarningsOnPropertyNames { get; private set; }

        public HashSet<string> IgnoreFormatsInMetadata { get; set; }

        public HashSet<string> AllowFileExtensions { get; set; }

        public string WebHostUrl { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public string ServiceStackHandlerFactoryPath { get; set; }
        public string DefaultRedirectPath { get; set; }
        public string MetadataRedirectPath { get; set; }

        public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
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

        public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; set; }

        public List<RouteNamingConventionDelegate> RouteNamingConventions { get; set; }

        public Dictionary<HttpStatusCode, IServiceStackHttpHandler> CustomHttpHandlers { get; set; }
        public IServiceStackHttpHandler GlobalHtmlErrorHttpHandler { get; set; }
        public Dictionary<Type, int> MapExceptionToStatusCode { get; set; }

        public bool OnlySendSessionCookiesSecurely { get; set; }
        public string RestrictAllCookiesToDomain { get; set; }

        public TimeSpan DefaultJsonpCacheExpiration { get; set; }
        public bool Return204NoContentForEmptyResponse { get; set; }
        public bool AllowPartialResponses { get; set; }
        public bool AllowNonHttpOnlyCookies { get; set; }
        public bool AllowAclUrlReservation { get; set; }

        public bool UseHttpsLinks { get; set; }

        public string AdminAuthSecret { get; set; }

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

            foreach (var operationType in this.Metadata.RequestTypes)
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
                throw new UnauthorizedAccessException(
                    String.Format("'{0}' Features have been disabled by your administrator", usesFeatures));
            }
        }

        public UnauthorizedAccessException UnauthorizedAccess(EndpointAttributes requestAttrs)
        {
            return new UnauthorizedAccessException(
                String.Format("Request with '{0}' is not allowed", requestAttrs));
        }

        public void AssertContentType(string contentType)
        {
            if (EndpointHost.Config.EnableFeatures == Feature.All) return;

            AssertFeatures(contentType.ToFeature());
        }

        public MetadataPagesConfig MetadataPagesConfig
        {
            get
            {
                return new MetadataPagesConfig(
                    Metadata,
                    ServiceEndpointsMetadataConfig,
                    IgnoreFormatsInMetadata,
                    EndpointHost.ContentTypeFilter.ContentTypeFormats.Keys.ToList());
            }
        }

        public bool HasAccessToMetadata(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            if (!HasFeature(Feature.Metadata))
            {
                EndpointHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Available");
                return false;
            }

            if (MetadataVisibility != EndpointAttributes.Any)
            {
                var actualAttributes = httpReq.GetAttributes();
                if ((actualAttributes & MetadataVisibility) != MetadataVisibility)
                {
                    HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Visible");
                    return false;
                }
            }
            return true;
        }

        public void HandleErrorResponse(IHttpRequest httpReq, IHttpResponse httpRes, HttpStatusCode errorStatus, string errorStatusDescription = null)
        {
            if (httpRes.IsClosed) return;

            httpRes.StatusDescription = errorStatusDescription;

            var handler = GetHandlerForErrorStatus(errorStatus);

            handler.ProcessRequest(httpReq, httpRes, httpReq.OperationName);
        }

        public IServiceStackHttpHandler GetHandlerForErrorStatus(HttpStatusCode errorStatus)
        {
            var httpHandler = GetCustomErrorHandler(errorStatus);

            switch (errorStatus)
            {
                case HttpStatusCode.Forbidden:
                    return httpHandler ?? new ForbiddenHttpHandler();
                case HttpStatusCode.NotFound:
                    return httpHandler ?? new NotFoundHttpHandler();
            }

            if (CustomHttpHandlers != null)
            {
                CustomHttpHandlers.TryGetValue(HttpStatusCode.NotFound, out httpHandler);
            }

            return httpHandler ?? new NotFoundHttpHandler();
        }

        public IServiceStackHttpHandler GetCustomErrorHandler(int errorStatusCode)
        {
            try
            {
                return GetCustomErrorHandler((HttpStatusCode)errorStatusCode);
            }
            catch
            {
                return null;
            }
        }

        public IServiceStackHttpHandler GetCustomErrorHandler(HttpStatusCode errorStatus)
        {
            IServiceStackHttpHandler httpHandler = null;
            if (CustomHttpHandlers != null)
            {
                CustomHttpHandlers.TryGetValue(errorStatus, out httpHandler);
            }
            return httpHandler ?? GlobalHtmlErrorHttpHandler;
        }

        public IHttpHandler GetCustomErrorHttpHandler(HttpStatusCode errorStatus)
        {
            var ssHandler = GetCustomErrorHandler(errorStatus);
            if (ssHandler == null) return null;
            var httpHandler = ssHandler as IHttpHandler;
            return httpHandler ?? new ServiceStackHttpHandler(ssHandler);
        }

        public Action<object, IHttpRequest, IHttpResponse> PreExecuteServiceFilter { get; set; }

        public Action<object, IHttpRequest, IHttpResponse> PostExecuteServiceFilter { get; set; }

        public FallbackRestPathDelegate FallbackRestPath { get; set; }

        public bool HasValidAuthSecret(IHttpRequest req)
        {
            if (AdminAuthSecret != null)
            {
                var authSecret = req.GetParam("authsecret");
                return authSecret == EndpointHost.Config.AdminAuthSecret;
            }

            return false;
        }
    }

}
