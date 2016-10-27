using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using MarkdownSharp;
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
        public static HostConfig Instance => instance ?? (instance = NewInstance());

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
                IgnoreFormatsInMetadata = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                },
                AllowFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "js", "ts", "tsx", "jsx", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", "pdf",
                    "jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", "svg",
                    "avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv",
                    "flv", "swf", "xap", "xaml", "ogg", "ogv", "mp4", "webm", "eot", "ttf", "woff", "woff2", "map"
                },
                DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
                DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
                EnableFeatures = Feature.All,
                WriteErrorsToResponse = true,
                ReturnsInnerException = true,
                DisposeDependenciesAfterUse = true,
                LogUnobservedTaskExceptions = true,
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
                AllowSessionIdsInHttpParams = false,
                AllowSessionCookies = true,
                RestrictAllCookiesToDomain = null,
                DefaultJsonpCacheExpiration = new TimeSpan(0, 20, 0),
                MetadataVisibility = RequestAttributes.Any,
                Return204NoContentForEmptyResponse = true,
                AllowJsConfig = true,
                AllowPartialResponses = true,
                AllowAclUrlReservation = true,
                AddRedirectParamsToQueryString = false,
                RedirectToDefaultDocuments = false,
                StripApplicationVirtualPath = false,
                ScanSkipPaths = new List<string> {
                    "obj/",
                    "bin/",
                    "node_modules/",
                    "jspm_packages/",
                    "bower_components/",
                    "wwwroot_build/",
#if !NETSTANDARD1_6 
                    "wwwroot/", //Need to allow VirtualFiles access from ContentRoot Folder
#endif
                },
                IgnoreWarningsOnPropertyNames = new List<string> {
                    Keywords.Format, Keywords.Callback, Keywords.Debug, Keywords.AuthSecret,
                    Keywords.IgnorePlaceHolder, Keywords.Version, Keywords.VersionAbbr, Keywords.Version.ToPascalCase(),
                },
                XmlWriterSettings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                },
                FallbackRestPath = null,
                UseHttpsLinks = false,
#if !NETSTANDARD1_6
                UseCamelCase = false,
                EnableOptimizations = false,
#else
                UseCamelCase = true,
                EnableOptimizations = true,
#endif
                DisableChunkedEncoding = false
            };

            Platform.Instance.InitHostConifg(config);

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
            this.DisposeDependenciesAfterUse = instance.DisposeDependenciesAfterUse;
            this.LogUnobservedTaskExceptions = instance.LogUnobservedTaskExceptions;
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
            this.AllowSessionIdsInHttpParams = instance.AllowSessionIdsInHttpParams;
            this.AllowSessionCookies = instance.AllowSessionCookies;
            this.RestrictAllCookiesToDomain = instance.RestrictAllCookiesToDomain;
            this.DefaultJsonpCacheExpiration = instance.DefaultJsonpCacheExpiration;
            this.MetadataVisibility = instance.MetadataVisibility;
            this.Return204NoContentForEmptyResponse = instance.Return204NoContentForEmptyResponse;
            this.AllowNonHttpOnlyCookies = instance.AllowNonHttpOnlyCookies;
            this.AllowJsConfig = instance.AllowJsConfig;
            this.AllowPartialResponses = instance.AllowPartialResponses;
            this.IgnoreWarningsOnPropertyNames = instance.IgnoreWarningsOnPropertyNames;
            this.FallbackRestPath = instance.FallbackRestPath;
            this.AllowAclUrlReservation = instance.AllowAclUrlReservation;
            this.AddRedirectParamsToQueryString = instance.AddRedirectParamsToQueryString;
            this.RedirectToDefaultDocuments = instance.RedirectToDefaultDocuments;
            this.StripApplicationVirtualPath = instance.StripApplicationVirtualPath;
            this.SkipFormDataInCreatingRequest = instance.SkipFormDataInCreatingRequest;
            this.ScanSkipPaths = instance.ScanSkipPaths;
            this.AdminAuthSecret = instance.AdminAuthSecret;
            this.UseHttpsLinks = instance.UseHttpsLinks;
            this.UseCamelCase = instance.UseCamelCase;
            this.EnableOptimizations = instance.EnableOptimizations;
            this.DisableChunkedEncoding = instance.DisableChunkedEncoding;
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
        internal string[] PreferredContentTypesArray = TypeConstants.EmptyStringArray; //use array at runtime
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
        public bool DisposeDependenciesAfterUse { get; set; }
        public bool LogUnobservedTaskExceptions { get; set; }

        public MarkdownOptions MarkdownOptions { get; set; }
        public Type MarkdownBaseType { get; set; }
        public Dictionary<string, Type> MarkdownGlobalHelpers { get; set; }
        public Dictionary<string, string> HtmlReplaceTokens { get; set; }

        public HashSet<string> AppendUtf8CharsetOnContentTypes { get; set; }

        public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

        public List<RouteNamingConventionDelegate> RouteNamingConventions { get; set; }

        public Dictionary<Type, int> MapExceptionToStatusCode { get; set; }

        public bool OnlySendSessionCookiesSecurely { get; set; }
        public bool AllowSessionIdsInHttpParams { get; set; }
        public bool AllowSessionCookies { get; set; }
        public string RestrictAllCookiesToDomain { get; set; }

        public TimeSpan DefaultJsonpCacheExpiration { get; set; }
        public bool Return204NoContentForEmptyResponse { get; set; }
        public bool AllowJsConfig { get; set; }
        public bool AllowPartialResponses { get; set; }
        public bool AllowNonHttpOnlyCookies { get; set; }
        public bool AllowAclUrlReservation { get; set; }
        public bool AddRedirectParamsToQueryString { get; set; }
        public bool RedirectToDefaultDocuments { get; set; }
        public bool StripApplicationVirtualPath { get; set; }
        public bool SkipFormDataInCreatingRequest { get; set; }

        //Skip scanning common VS.NET extensions
        public List<string> ScanSkipPaths { get; private set; }

        public bool UseHttpsLinks { get; set; }

        public bool UseCamelCase { get; set; }
        public bool EnableOptimizations { get; set; }

        //Disables chunked encoding on Kestrel Server
        public bool DisableChunkedEncoding { get; set; }

        public string AdminAuthSecret { get; set; }

        public FallbackRestPathDelegate FallbackRestPath { get; set; }

        private HashSet<string> razorNamespaces;
        public HashSet<string> RazorNamespaces => razorNamespaces 
            ?? (razorNamespaces = Platform.Instance.GetRazorNamespaces());

    }

}
