using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Metadata;
using ServiceStack.Text;
using ServiceStack.Web;

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
                IsMobileRegex = new Regex("Mobile|iP(hone|od|ad)|Android|BlackBerry|IEMobile|Kindle|(hpw|web)OS|Fennec|Minimo|Opera M(obi|ini)|Blazer|Dolfin|Dolphin|Skyfire|Zune", RegexOptions.Compiled),
                RequestRules = new Dictionary<string, Func<IHttpRequest, bool>> {
                    {"AcceptsHtml", req => req.Accept?.IndexOf(MimeTypes.Html, StringComparison.Ordinal) >= 0 },
                    {"AcceptsJson", req => req.Accept?.IndexOf(MimeTypes.Json, StringComparison.Ordinal) >= 0 },
                    {"AcceptsXml", req => req.Accept?.IndexOf(MimeTypes.Xml, StringComparison.Ordinal) >= 0 },
                    {"AcceptsJsv", req => req.Accept?.IndexOf(MimeTypes.Jsv, StringComparison.Ordinal) >= 0 },
                    {"AcceptsCsv", req => req.Accept?.IndexOf(MimeTypes.Csv, StringComparison.Ordinal) >= 0 },
                    {"IsAuthenticated", req => req.IsAuthenticated() },
                    {"IsMobile", req => Instance.IsMobileRegex.IsMatch(req.UserAgent) },
                    {"{int}/**", req => int.TryParse(req.PathInfo.Substring(1).LeftPart('/'), out _) },
                    {"path/{int}/**", req => {
                        var afterFirst = req.PathInfo.Substring(1).RightPart('/');
                        return !string.IsNullOrEmpty(afterFirst) && int.TryParse(afterFirst.LeftPart('/'), out _);
                    }},
                    {"**/{int}", req => int.TryParse(req.PathInfo.LastRightPart('/'), out _) },
                    {"**/{int}/path", req => {
                        var beforeLast = req.PathInfo.LastLeftPart('/');
                        return !string.IsNullOrEmpty(beforeLast) && int.TryParse(beforeLast.LastRightPart('/'), out _);
                    }},
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
                CompressFilesWithExtensions = new HashSet<string>(),
                AllowFilePaths = new List<string>
                {
                    "jspm_packages/**/*.json"
                },
                ForbiddenPaths = new List<string>(),
                DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
                DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
                EnableFeatures = Feature.All,
                WriteErrorsToResponse = true,
                ReturnsInnerException = true,
                DisposeDependenciesAfterUse = true,
                LogUnobservedTaskExceptions = true,
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
                UseSaltedHash = false,
                FallbackPasswordHashers = new List<IPasswordHasher>(),
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
                RedirectDirectoriesToTrailingSlashes = true,
                StripApplicationVirtualPath = false,
                ScanSkipPaths = new List<string> {
                    "obj/",
                    "bin/",
                    "node_modules/",
                    "jspm_packages/",
                    "bower_components/",
                    "wwwroot_build/",
#if !NETSTANDARD2_0 
                    "wwwroot/", //Need to allow VirtualFiles access from ContentRoot Folder
#endif
                },
                RedirectPaths = new Dictionary<string, string>
                {
                    { "/metadata/", "/metadata" },
                },
                IgnoreWarningsOnPropertyNames = new List<string> {
                    Keywords.Format, Keywords.Callback, Keywords.Debug, Keywords.AuthSecret, Keywords.JsConfig,
                    Keywords.IgnorePlaceHolder, Keywords.Version, Keywords.VersionAbbr, Keywords.Version.ToPascalCase(),
                    Keywords.ApiKeyParam, Keywords.Code, 
                },
                XmlWriterSettings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                },
                FallbackRestPath = null,
                UseHttpsLinks = false,
#if !NETSTANDARD2_0
                UseCamelCase = false,
                EnableOptimizations = false,
#else
                UseCamelCase = true,
                EnableOptimizations = true,
#endif
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
            this.StrictMode = instance.StrictMode;
            this.DefaultDocuments = instance.DefaultDocuments;
            this.IsMobileRegex = instance.IsMobileRegex;
            this.RequestRules = instance.RequestRules;
            this.GlobalResponseHeaders = instance.GlobalResponseHeaders;
            this.IgnoreFormatsInMetadata = instance.IgnoreFormatsInMetadata;
            this.AllowFileExtensions = instance.AllowFileExtensions;
            this.CompressFilesWithExtensions = instance.CompressFilesWithExtensions;
            this.CompressFilesLargerThanBytes = instance.CompressFilesLargerThanBytes;
            this.AllowFilePaths = instance.AllowFilePaths;
            this.ForbiddenPaths = instance.ForbiddenPaths;
            this.EnableFeatures = instance.EnableFeatures;
            this.WriteErrorsToResponse = instance.WriteErrorsToResponse;
            this.DisposeDependenciesAfterUse = instance.DisposeDependenciesAfterUse;
            this.LogUnobservedTaskExceptions = instance.LogUnobservedTaskExceptions;
            this.ReturnsInnerException = instance.ReturnsInnerException;
            this.HtmlReplaceTokens = instance.HtmlReplaceTokens;
            this.AddMaxAgeForStaticMimeTypes = instance.AddMaxAgeForStaticMimeTypes;
            this.AppendUtf8CharsetOnContentTypes = instance.AppendUtf8CharsetOnContentTypes;
            this.RouteNamingConventions = instance.RouteNamingConventions;
            this.MapExceptionToStatusCode = instance.MapExceptionToStatusCode;
            this.UseSaltedHash = instance.UseSaltedHash;
            this.FallbackPasswordHashers = instance.FallbackPasswordHashers;
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
            this.IgnoreWarningsOnAllProperties = instance.IgnoreWarningsOnAllProperties;
            this.IgnoreWarningsOnPropertyNames = instance.IgnoreWarningsOnPropertyNames;
            this.FallbackRestPath = instance.FallbackRestPath;
            this.AllowAclUrlReservation = instance.AllowAclUrlReservation;
            this.AddRedirectParamsToQueryString = instance.AddRedirectParamsToQueryString;
            this.RedirectToDefaultDocuments = instance.RedirectToDefaultDocuments;
            this.RedirectDirectoriesToTrailingSlashes = instance.RedirectDirectoriesToTrailingSlashes;
            this.StripApplicationVirtualPath = instance.StripApplicationVirtualPath;
            this.SkipFormDataInCreatingRequest = instance.SkipFormDataInCreatingRequest;
            this.ScanSkipPaths = instance.ScanSkipPaths;
            this.RedirectPaths = instance.RedirectPaths;
            this.AdminAuthSecret = instance.AdminAuthSecret;
            this.UseHttpsLinks = instance.UseHttpsLinks;
            this.UseCamelCase = instance.UseCamelCase;
            this.EnableOptimizations = instance.EnableOptimizations;
        }

        public string WsdlServiceNamespace { get; set; }
        public string ApiVersion { get; set; }

        private RequestAttributes metadataVisibility;
        public RequestAttributes MetadataVisibility
        {
            get => metadataVisibility;
            set => metadataVisibility = value.ToAllowedFlagsSet();
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

        private bool? strictMode;
        public bool? StrictMode
        {
            get => strictMode;
            set => Env.StrictMode = (strictMode = value).GetValueOrDefault();
        }
        
        public string DebugAspNetHostEnvironment { get; set; }
        public string DebugHttpListenerHostEnvironment { get; set; }
        public List<string> DefaultDocuments { get; private set; }

        public bool IgnoreWarningsOnAllProperties { get; set; }
        public List<string> IgnoreWarningsOnPropertyNames { get; private set; }

        public HashSet<string> IgnoreFormatsInMetadata { get; set; }

        public HashSet<string> AllowFileExtensions { get; set; }
        public HashSet<string> CompressFilesWithExtensions { get; set; }
        public long? CompressFilesLargerThanBytes { get; set; }
        public List<string> ForbiddenPaths { get; set; }
        public List<string> AllowFilePaths { get; set; }

        public string WebHostUrl { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public string HandlerFactoryPath { get; set; }
        public string DefaultRedirectPath { get; set; }
        public string MetadataRedirectPath { get; set; }

        public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
        public string SoapServiceName { get; set; }
        public XmlWriterSettings XmlWriterSettings { get; set; }
        public bool EnableAccessRestrictions { get; set; }
        public bool UseBclJsonSerializers { get; set; }
        public Regex IsMobileRegex { get; set; }
        public Dictionary<string, Func<IHttpRequest, bool>> RequestRules { get; set; }
        public Dictionary<string, string> GlobalResponseHeaders { get; set; }
        public Feature EnableFeatures { get; set; }
        public bool ReturnsInnerException { get; set; }
        public bool WriteErrorsToResponse { get; set; }
        public bool DisposeDependenciesAfterUse { get; set; }
        public bool LogUnobservedTaskExceptions { get; set; }

        [Obsolete("Use LogManager.LogFactory")]
        public ILogFactory LogFactory
        {
            get => LogManager.LogFactory;
            set => LogManager.LogFactory = value;
        }

        public Dictionary<string, string> HtmlReplaceTokens { get; set; }

        public HashSet<string> AppendUtf8CharsetOnContentTypes { get; set; }

        public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

        public List<RouteNamingConventionDelegate> RouteNamingConventions { get; set; }

        public Dictionary<Type, int> MapExceptionToStatusCode { get; set; }

        /// <summary>
        /// If enabled reverts to persist password hashes using the original SHA256 SaltedHash implementation. 
        /// By default ServiceStack uses the more secure ASP.NET Identity v3 PBKDF2 with HMAC-SHA256 implementation.
        /// 
        /// New Users will have their passwords persisted with the specified implementation, likewise existing users will have their passwords re-hased
        /// to use the current registered IPasswordHasher.
        /// </summary>
        public bool UseSaltedHash { get; set; }

        /// <summary>
        /// Older Password Hashers that were previously used to hash passwords. Failed password matches check to see if the password was hashed with 
        /// any of the registered FallbackPasswordHashers, if true the password attempt will succeed and password will get re-hashed with 
        /// the current registered IPasswordHasher.
        /// </summary>
        public List<IPasswordHasher> FallbackPasswordHashers { get; private set; }

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
        public bool RedirectDirectoriesToTrailingSlashes { get; set; }

        //Skip scanning common VS.NET extensions
        public List<string> ScanSkipPaths { get; private set; }

        public Dictionary<string,string> RedirectPaths { get; private set; }

        public bool UseHttpsLinks { get; set; }

        public bool UseCamelCase { get; set; }
        public bool EnableOptimizations { get; set; }

        public string AdminAuthSecret { get; set; }

        public FallbackRestPathDelegate FallbackRestPath { get; set; }

        private HashSet<string> razorNamespaces;
        public HashSet<string> RazorNamespaces => razorNamespaces 
            ?? (razorNamespaces = Platform.Instance.GetRazorNamespaces());

    }

}
