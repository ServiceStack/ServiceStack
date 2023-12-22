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

namespace ServiceStack;

public class HostConfig
{
    public const string DefaultWsdlNamespace = "http://schemas.servicestack.net/types";
    public static string ServiceStackPath = null;

    private static HostConfig instance;
    public static HostConfig Instance => instance ??= NewInstance();

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
            EmbeddedResourceSources = new(),
            EmbeddedResourceBaseTypes = new[] { HostContext.AppHost.GetType(), typeof(Service) }.ToList(),
            EmbeddedResourceTreatAsFiles = new(),
            EnableAccessRestrictions = true,
            EnableAutoHtmlResponses = true,
            WebHostPhysicalPath = "~".MapServerPath(),
            HandlerFactoryPath = ServiceStackPath,
            MetadataRedirectPath = null,
            DefaultContentType = null,
            PreferredContentTypes = new() {
                MimeTypes.Html, MimeTypes.Json, MimeTypes.Xml, MimeTypes.Jsv
            },
            AllowJsonpRequests = true,
            AllowRouteContentTypeExtensions = true,
            BufferSyncSerializers = Env.IsNetCore3,
            DebugMode = false,
            StrictMode = Env.StrictMode,
            DefaultDocuments = new() {
                "default.htm",
                "default.html",
                "default.cshtml",
                "default.md",
                "index.htm",
                "index.html",
                "default.aspx",
                "default.ashx",
            },
            GlobalResponseHeaders = new() {
                { HttpHeaders.Vary, "Accept" },
                { HttpHeaders.XPoweredBy, Env.ServerUserAgent },
            },
            IsMobileRegex = new Regex("Mobile|iP(hone|od|ad)|Android|BlackBerry|IEMobile|Kindle|(hpw|web)OS|Fennec|Minimo|Opera M(obi|ini)|Blazer|Dolfin|Dolphin|Skyfire|Zune", RegexOptions.Compiled),
            RequestRules = new() {
                {"AcceptsHtml", req => req.Accept?.IndexOf(MimeTypes.Html, StringComparison.Ordinal) >= 0 },
                {"AcceptsJson", req => req.Accept?.IndexOf(MimeTypes.Json, StringComparison.Ordinal) >= 0 },
                {"AcceptsXml", req => req.Accept?.IndexOf(MimeTypes.Xml, StringComparison.Ordinal) >= 0 },
                {"AcceptsJsv", req => req.Accept?.IndexOf(MimeTypes.Jsv, StringComparison.Ordinal) >= 0 },
                {"AcceptsCsv", req => req.Accept?.IndexOf(MimeTypes.Csv, StringComparison.Ordinal) >= 0 },
#pragma warning disable CS0618
                {"IsAuthenticated", req => req.IsAuthenticated() },
#pragma warning restore CS0618
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
            IgnoreFormatsInMetadata = new(StringComparer.OrdinalIgnoreCase)
            {
            },
            AllowFileExtensions = new(StringComparer.OrdinalIgnoreCase)
            {
                "js", "ts", "tsx", "jsx", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", "pdf",
                "jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", "svg", "webp", "jif", "jfif", "jpe",
                "avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv",
                "flv", "swf", "xap", "xaml", "ogg", "ogv", "mp4", "webm", "eot", "ttf", "woff", "woff2", "map",
                "xls", "xla", "xlsx", "xltx", "doc", "dot", "docx", "dotx", "ppt", "pps", "ppa", "pptx", "potx", 
                "wasm", "proto", "cer", "crt", "webmanifest", "mjs", "cjs", 
            },
            CompressFilesWithExtensions = new(),
            AllowFilePaths = new() {
                "jspm_packages/**/*.json", //JSPM
                ".well-known/**/*",        //LetsEncrypt
            },
            IgnorePathInfoPrefixes = new(),
            ForbiddenPaths = new(),
            DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
            DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
            EnableFeatures = Feature.All,
            WriteErrorsToResponse = true,
            DisposeDependenciesAfterUse = true,
            LogUnobservedTaskExceptions = true,
            HtmlReplaceTokens = new(),
            AddMaxAgeForStaticMimeTypes = new() {
                { "image/gif", TimeSpan.FromHours(1) },
                { "image/png", TimeSpan.FromHours(1) },
                { "image/jpeg", TimeSpan.FromHours(1) },
            },
            AppendUtf8CharsetOnContentTypes = new() { MimeTypes.Json },
            RouteNamingConventions = new() {
                RouteNamingConvention.WithRequestDtoName,
                RouteNamingConvention.WithMatchingAttributes,
                RouteNamingConvention.WithMatchingPropertyNames
            },
            MapExceptionToStatusCode = new(),
            UseSaltedHash = false,
            FallbackPasswordHashers = new(),
            UseSameSiteCookies = null,
            UseSecureCookies = true,   // good default to have, but needed if UseSameSiteCookies=true
            UseHttpOnlyCookies = true,
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
            ScanSkipPaths = new() {
                "obj/",
                "bin/",
                "node_modules/",
                "jspm_packages/",
                "bower_components/",
                "wwwroot_build/",
#if !NETCORE 
                    "wwwroot/", //Need to allow VirtualFiles access from ContentRoot Folder
#endif
            },
            RedirectPaths = new Dictionary<string, string>
            {
                { "/metadata/", "/metadata" },
            },
            IgnoreWarningsOnPropertyNames = new(StringComparer.OrdinalIgnoreCase) {
                Keywords.Format, Keywords.Callback, Keywords.Debug, Keywords.AuthSecret, Keywords.JsConfig,
                Keywords.IgnorePlaceHolder, Keywords.ApiKeyParam, Keywords.Code, Keywords.Redirect, Keywords.Continue, 
                Keywords.SessionState, Keywords.Version, Keywords.Version, Keywords.VersionAbbr, Keywords.VersionFxAbbr,
                Keywords.OAuthSuccess, Keywords.OAuthFailed, Keywords.WithoutOptions,
            },
            IgnoreWarningsOnAutoQueryApis = true,
            XmlWriterSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            },
            FallbackRestPath = null,
            UseHttpsLinks = false,
            UseJsObject = true,
            EnableOptimizations = true,
            TreatNonNullableRefTypesAsRequired = true,
            AuthSecretSession = new AuthUserSession {
                Id = Guid.NewGuid().ToString("n"),
                DisplayName = "Admin",
                UserName = Keywords.AuthSecret,
                UserAuthName = Keywords.AuthSecret,
                AuthProvider = Keywords.AuthSecret,
                IsAuthenticated = true,
                Roles = new() { Configuration.RoleNames.Admin },
                Permissions = new(),
                UserAuthId = "0",
            },
#if !NETCORE
                UseCamelCase = false,
                ReturnsInnerException = true,
#else
            UseCamelCase = true,
            ReturnsInnerException = false,
#endif
        };

        Platform.Instance.InitHostConfig(config);

        return config;
    }

    public HostConfig()
    {
        if (instance == null) return;

        //Get a copy of the singleton already partially configured
        this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
        this.ApiVersion = instance.ApiVersion;
        this.AppInfo = instance.AppInfo;
        this.EmbeddedResourceSources = instance.EmbeddedResourceSources;
        this.EmbeddedResourceBaseTypes = instance.EmbeddedResourceBaseTypes;
        this.EmbeddedResourceTreatAsFiles = instance.EmbeddedResourceTreatAsFiles;
        this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
        this.EnableAutoHtmlResponses = instance.EnableAutoHtmlResponses;
        this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
        this.SoapServiceName = instance.SoapServiceName;
        this.XmlWriterSettings = instance.XmlWriterSettings;
        this.WebHostUrl = instance.WebHostUrl;
        this.WebHostPhysicalPath = instance.WebHostPhysicalPath;
        this.DefaultRedirectPath = instance.DefaultRedirectPath;
        this.MetadataRedirectPath = instance.MetadataRedirectPath;
        this.HandlerFactoryPath = instance.HandlerFactoryPath;
        this.DefaultContentType = instance.DefaultContentType;
        this.PreferredContentTypes = instance.PreferredContentTypes;
        this.AllowJsonpRequests = instance.AllowJsonpRequests;
        this.AllowRouteContentTypeExtensions = instance.AllowRouteContentTypeExtensions;
        this.BufferSyncSerializers = instance.BufferSyncSerializers;
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
        this.IgnorePathInfoPrefixes = instance.IgnorePathInfoPrefixes;
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
        this.UseSecureCookies = instance.UseSecureCookies;
        this.AllowSessionIdsInHttpParams = instance.AllowSessionIdsInHttpParams;
        this.AllowSessionCookies = instance.AllowSessionCookies;
        this.RestrictAllCookiesToDomain = instance.RestrictAllCookiesToDomain;
        this.DefaultJsonpCacheExpiration = instance.DefaultJsonpCacheExpiration;
        this.MetadataVisibility = instance.MetadataVisibility;
        this.Return204NoContentForEmptyResponse = instance.Return204NoContentForEmptyResponse;
        this.UseHttpOnlyCookies = instance.UseHttpOnlyCookies;
        this.UseSameSiteCookies = instance.UseSameSiteCookies;
        this.AllowJsConfig = instance.AllowJsConfig;
        this.AllowPartialResponses = instance.AllowPartialResponses;
        this.IgnoreWarningsOnAllProperties = instance.IgnoreWarningsOnAllProperties;
        this.IgnoreWarningsOnAutoQueryApis = instance.IgnoreWarningsOnAutoQueryApis;
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
        this.AuthSecretSession = instance.AuthSecretSession;
        this.UseHttpsLinks = instance.UseHttpsLinks;
        this.UseCamelCase = instance.UseCamelCase;
        this.UseJsObject = instance.UseJsObject;
        this.EnableOptimizations = instance.EnableOptimizations;
        this.TreatNonNullableRefTypesAsRequired = instance.TreatNonNullableRefTypesAsRequired;
    }

    public string WsdlServiceNamespace { get; set; }
    public string ApiVersion { get; set; }
        
    public AppInfo AppInfo { get; set; }

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
    public bool BufferSyncSerializers { get; set; }

    public bool DebugMode { get; set; }
    public bool? StrictMode { get; set; }

    public string DebugAspNetHostEnvironment { get; set; }
    public string DebugHttpListenerHostEnvironment { get; set; }
    public List<string> DefaultDocuments { get; private set; }

    public bool IgnoreWarningsOnAllProperties { get; set; }
    public bool IgnoreWarningsOnAutoQueryApis { get; set; }
    public HashSet<string> IgnoreWarningsOnPropertyNames { get; private set; }

    public HashSet<string> IgnoreFormatsInMetadata { get; set; }

    public HashSet<string> AllowFileExtensions { get; set; }
    public HashSet<string> CompressFilesWithExtensions { get; set; }
    public long? CompressFilesLargerThanBytes { get; set; }
    public List<string> ForbiddenPaths { get; set; }
    public List<string> AllowFilePaths { get; set; }
    /// <summary>
    /// Ignore handling requests with matching /path/info prefixes 
    /// </summary>
    public List<string> IgnorePathInfoPrefixes { get; set; }

    public string WebHostUrl { get; set; }
    public string WebHostPhysicalPath { get; set; }
    public string HandlerFactoryPath { get; set; }
    public string PathBase { get; internal set; } // auto populated from HandlerFactoryPath
    public string DefaultRedirectPath { get; set; }
    public string MetadataRedirectPath { get; set; }

    public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
    public string SoapServiceName { get; set; }
    public XmlWriterSettings XmlWriterSettings { get; set; }
    public bool EnableAccessRestrictions { get; set; }
    public bool EnableAutoHtmlResponses { get; set; }
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
    public bool AllowSessionIdsInHttpParams { get; set; }
    public bool AllowSessionCookies { get; set; }
    public string RestrictAllCookiesToDomain { get; set; }

    public TimeSpan DefaultJsonpCacheExpiration { get; set; }
    public bool Return204NoContentForEmptyResponse { get; set; }
    public bool AllowJsConfig { get; set; }
    public bool AllowPartialResponses { get; set; }

    [Obsolete("Use !UseHttpOnlyCookies")]
    public bool AllowNonHttpOnlyCookies { set => UseHttpOnlyCookies = !value; }
    [Obsolete("Use UseSecureCookies")]
    public bool OnlySendSessionCookiesSecurely { set => UseSecureCookies = value; }

    public bool UseSecureCookies { get; set; }
    public bool UseHttpOnlyCookies { get; set; }
    /// <summary>
    /// Configure cookies to use SameSite=[null:Lax,true:Strict,false:None]
    /// </summary>
    public bool? UseSameSiteCookies { get; set; }
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
    public bool UseJsObject { get; set; }
    public bool EnableOptimizations { get; set; }
    public bool TreatNonNullableRefTypesAsRequired { get; set; }

    public string AdminAuthSecret { get; set; }
        
    public IAuthSession AuthSecretSession { get; set; }

    public FallbackRestPathDelegate FallbackRestPath { get; set; }

    private HashSet<string> razorNamespaces;
    public HashSet<string> RazorNamespaces => razorNamespaces ??= Platform.Instance.GetRazorNamespaces();

}