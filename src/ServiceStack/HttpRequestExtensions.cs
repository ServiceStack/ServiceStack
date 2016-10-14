using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;
using static System.String;

#if !NETSTANDARD1_6
using ServiceStack.Host.AspNet;
using ServiceStack.Host.HttpListener;
#endif

namespace ServiceStack
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Gets string value from Items[name] then Cookies[name] if exists.
        /// Useful when *first* setting the users response cookie in the request filter.
        /// To access the value for this initial request you need to set it in Items[].
        /// </summary>
        /// <returns>string value or null if it doesn't exist</returns>
        public static string GetItemOrCookie(this IRequest httpReq, string name)
        {
            object value;
            if (httpReq.Items.TryGetValue(name, out value)) return value.ToString();

            Cookie cookie;
            if (httpReq.Cookies.TryGetValue(name, out cookie)) return cookie.Value;

            return null;
        }

        /// <summary>
        /// Gets request paramater string value by looking in the following order:
        /// - QueryString[name]
        /// - FormData[name]
        /// - Cookies[name]
        /// - Items[name]
        /// </summary>
        /// <returns>string value or null if it doesn't exist</returns>
        public static string GetParam(this IRequest httpReq, string name)
        {
            string value;
            if ((value = httpReq.Headers[HttpHeaders.XParamOverridePrefix + name]) != null) return value;
            if ((value = httpReq.QueryString[name]) != null) return value;
            if ((value = httpReq.FormData[name]) != null) return value;

            //IIS will assign null to params without a name: .../?some_value can be retrieved as req.Params[null]
            //TryGetValue is not happy with null dictionary keys, so we should bail out here
            if (IsNullOrEmpty(name)) return null;

            Cookie cookie;
            if (httpReq.Cookies.TryGetValue(name, out cookie)) return cookie.Value;

            object oValue;
            if (httpReq.Items.TryGetValue(name, out oValue)) return oValue.ToString();

            return null;
        }

        public static string GetParentAbsolutePath(this IRequest httpReq)
        {
            return httpReq.GetAbsolutePath().ToParentPath();
        }

        public static string GetAbsolutePath(this IRequest httpReq)
        {
            var resolvedPathInfo = httpReq.PathInfo;

            var pos = httpReq.RawUrl.IndexOf(resolvedPathInfo, StringComparison.OrdinalIgnoreCase);
            if (pos == -1)
                throw new ArgumentException($"PathInfo '{resolvedPathInfo}' is not in Url '{httpReq.RawUrl}'");

            return httpReq.RawUrl.Substring(0, pos + resolvedPathInfo.Length);
        }

        public static string GetParentPathUrl(this IRequest httpReq)
        {
            return httpReq.GetPathUrl().ToParentPath();
        }

        public static string GetPathUrl(this IRequest httpReq)
        {
            var resolvedPathInfo = httpReq.PathInfo.TrimEnd('/');

            int pos;

            if (resolvedPathInfo == Empty)
            {
                pos = httpReq.AbsoluteUri.IndexOf('?');
                if (pos == -1)
                    pos = httpReq.AbsoluteUri.Length;
            }
            else
            {
                pos = httpReq.AbsoluteUri.IndexOf(resolvedPathInfo, StringComparison.OrdinalIgnoreCase);
            }

            if (pos == -1)
                throw new ArgumentException($"PathInfo '{resolvedPathInfo}' is not in Url '{httpReq.RawUrl}'");

            return httpReq.AbsoluteUri.Substring(0, pos + resolvedPathInfo.Length);
        }

        public static string GetUrlHostName(this IRequest httpReq)
        {
#if !NETSTANDARD1_6
            var aspNetReq = httpReq as ServiceStack.Host.AspNet.AspNetRequest;
            if (aspNetReq != null)
            {
                return aspNetReq.UrlHostName;
            }
#endif
            var uri = httpReq.AbsoluteUri;

            var pos = uri.IndexOf("://", StringComparison.Ordinal) + "://".Length;
            var partialUrl = uri.Substring(pos);
            var endPos = partialUrl.IndexOf('/');
            if (endPos == -1) endPos = partialUrl.Length;
            var hostName = partialUrl.Substring(0, endPos).Split(':')[0];
            return hostName;
        }

        public static string GetPhysicalPath(this IRequest httpReq) => HostContext.ResolvePhysicalPath(httpReq.PathInfo, httpReq);

        public static IVirtualFile GetVirtualFile(this IRequest httpReq) => HostContext.ResolveVirtualFile(httpReq.PathInfo, httpReq);

        public static IVirtualDirectory GetVirtualDirectory(this IRequest httpReq) => HostContext.ResolveVirtualDirectory(httpReq.PathInfo, httpReq);

        public static IVirtualNode GetVirtualNode(this IRequest httpReq) => HostContext.ResolveVirtualNode(httpReq.PathInfo, httpReq);

        public static string GetDirectoryPath(this IRequest request)
        {
            if (request == null)
                return null;

            var path = request.PathInfo;
            return IsNullOrEmpty(path) || path[path.Length - 1] == '/'
                ? path
                : path.Substring(0, path.LastIndexOf('/') + 1);
        }

        public static string GetLeftAuthority(this Uri uri)
        {
            return $"{uri.Scheme}://{uri.Authority}";
        }

        public static string GetApplicationUrl(this IRequest httpReq)
        {
            var url = new Uri(httpReq.AbsoluteUri);
            var baseUrl = url.GetLeftAuthority();
            var appUrl = baseUrl.CombineWith(HostContext.Config.HandlerFactoryPath);
            return appUrl;
        }

        public static string GetHttpMethodOverride(this IRequest httpReq)
        {
            var httpMethod = httpReq.Verb;

            if (httpMethod != HttpMethods.Post)
                return httpMethod;

            var overrideHttpMethod =
                httpReq.Headers[HttpHeaders.XHttpMethodOverride].ToNullIfEmpty()
                ?? httpReq.FormData[HttpHeaders.XHttpMethodOverride].ToNullIfEmpty()
                ?? httpReq.QueryString[HttpHeaders.XHttpMethodOverride].ToNullIfEmpty();

            if (overrideHttpMethod != null)
            {
                if (overrideHttpMethod != HttpMethods.Get && overrideHttpMethod != HttpMethods.Post)
                    httpMethod = overrideHttpMethod;
            }

            return httpMethod;
        }

        public static string GetFormatModifier(this IRequest httpReq)
        {
            var format = httpReq.QueryString[Keywords.Format];
            if (format == null)
                return null;

            var pos = format.IndexOf('.');
            return pos >= 0 ? format.Substring(pos + 1) : null;
        }

        public static bool HasNotModifiedSince(this IRequest httpReq, DateTime? dateTime)
        {
            if (!dateTime.HasValue) return false;
            var strHeader = httpReq.Headers[HttpHeaders.IfModifiedSince];
            try
            {
                if (strHeader != null)
                {
                    var dateIfModifiedSince = DateTime.ParseExact(strHeader, "r", null);
                    var utcFromDate = dateTime.Value.ToUniversalTime();
                    //strip ms
                    utcFromDate = new DateTime(
                        utcFromDate.Ticks - (utcFromDate.Ticks % TimeSpan.TicksPerSecond),
                        utcFromDate.Kind
                    );

                    return utcFromDate <= dateIfModifiedSince;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool DidReturn304NotModified(this IRequest httpReq, DateTime? dateTime, IResponse httpRes)
        {
            if (httpReq.HasNotModifiedSince(dateTime))
            {
                httpRes.StatusCode = (int)HttpStatusCode.NotModified;
                return true;
            }
            return false;
        }

        public static string GetJsonpCallback(this IRequest httpReq)
        {
            return httpReq?.QueryString[Keywords.Callback];
        }


        public static Dictionary<string, string> CookiesAsDictionary(this IRequest httpReq)
        {
            return Platform.Instance.GetCookiesAsDictionary(httpReq);
        }

        public static int ToStatusCode(this Exception ex)
        {
            var hasStatusCode = ex as IHasStatusCode;
            if (hasStatusCode != null)
                return hasStatusCode.StatusCode;

            if (HostContext.Config != null)
            {
                var exType = ex.GetType();
                foreach (var entry in HostContext.Config.MapExceptionToStatusCode)
                {
                    if (entry.Key.IsAssignableFromType(exType))
                        return entry.Value;
                }
            }

            if (ex is HttpError) return ((HttpError)ex).Status;
            if (ex is NotImplementedException || ex is NotSupportedException) return (int)HttpStatusCode.MethodNotAllowed;
            if (ex is ArgumentException || ex is SerializationException || ex is FormatException) return (int)HttpStatusCode.BadRequest;
            if (ex is AuthenticationException) return (int)HttpStatusCode.Unauthorized;
            if (ex is UnauthorizedAccessException) return (int)HttpStatusCode.Forbidden;
            if (ex is OptimisticConcurrencyException) return (int)HttpStatusCode.Conflict;
            return (int)HttpStatusCode.InternalServerError;
        }

        public static string ToErrorCode(this Exception ex)
        {
            var hasErrorCode = ex as IHasErrorCode;
            return hasErrorCode?.ErrorCode
                ?? ex.GetType().Name;
        }

        public static WebServiceException ToWebServiceException(this HttpError error)
        {
            var to = new WebServiceException(error.Message, error.InnerException)
            {
                StatusCode = error.Status,
                StatusDescription = error.StatusDescription,
                ResponseDto = error.Response,
            };

            return to;
        }


        /**
         * 
             Input: http://localhost:96/Cambia3/Temp/Test.aspx/path/info?q=item#fragment

            Some HttpRequest path and URL properties:
            Request.ApplicationPath:	/Cambia3
            Request.CurrentExecutionFilePath:	/Cambia3/Temp/Test.aspx
            Request.FilePath:			/Cambia3/Temp/Test.aspx
            Request.Path:				/Cambia3/Temp/Test.aspx/path/info
            Request.PathInfo:			/path/info
            Request.PhysicalApplicationPath:	D:\Inetpub\wwwroot\CambiaWeb\Cambia3\
            Request.QueryString:		/Cambia3/Temp/Test.aspx/path/info?query=arg
            Request.Url.AbsolutePath:	/Cambia3/Temp/Test.aspx/path/info
            Request.Url.AbsoluteUri:	http://localhost:96/Cambia3/Temp/Test.aspx/path/info?query=arg
            Request.Url.Fragment:	
            Request.Url.Host:			localhost
            Request.Url.LocalPath:		/Cambia3/Temp/Test.aspx/path/info
            Request.Url.PathAndQuery:	/Cambia3/Temp/Test.aspx/path/info?query=arg
            Request.Url.Port:			96
            Request.Url.Query:			?query=arg
            Request.Url.Scheme:			http
            Request.Url.Segments:		/
                                        Cambia3/
                                        Temp/
                                        Test.aspx/
                                        path/
                                        info
         * */

        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpRequestExtensions));

        private static readonly string WebHostDirectoryName = "";

        static HttpRequestExtensions()
        {
            WebHostDirectoryName = Path.GetFileName("~".MapHostAbsolutePath());
        }

        public static string GetOperationNameFromLastPathInfo(string lastPathInfo)
        {
            if (IsNullOrEmpty(lastPathInfo)) return null;

            var operationName = lastPathInfo.Substring("/".Length);

            return operationName;
        }

        private static string GetLastPathInfoFromRawUrl(string rawUrl)
        {
            var pathInfo = rawUrl.IndexOf("?", StringComparison.Ordinal) != -1
                ? rawUrl.Substring(0, rawUrl.IndexOf("?", StringComparison.Ordinal))
                : rawUrl;

            pathInfo = pathInfo.Substring(pathInfo.LastIndexOf("/", StringComparison.Ordinal));

            return pathInfo;
        }

#if NETSTANDARD1_6
        public static string GetLastPathInfo(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var rawUrl = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request);
            return GetLastPathInfoFromRawUrl(rawUrl);
        }

        public static string GetOperationName(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var pathInfo = request.GetLastPathInfo();
            return GetOperationNameFromLastPathInfo(pathInfo);
        }
#endif

        public static string GetParentBaseUrl(this IRequest request)
        {
            var rawUrl = request.RawUrl;
            var endpointsPath = rawUrl.Substring(0, rawUrl.LastIndexOf('/') + 1);
            return new Uri(request.AbsoluteUri).GetLeftAuthority() + endpointsPath;
        }

#if !NETSTANDARD1_6
        //http://stackoverflow.com/a/757251/85785
        static readonly string[] VirtualPathPrefixes = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath == null || System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath == "/"
            ? TypeConstants.EmptyStringArray
            : new[] { System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath, "~" + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath };

        public static string SanitizedVirtualPath(this string virtualPath)
        {
            return HostContext.Config.StripApplicationVirtualPath
                ? virtualPath.TrimPrefixes(VirtualPathPrefixes)
                : virtualPath;
        }

        // http://localhost/ServiceStack.Examples.Host.Web/Public/Public/Soap12/Wsdl => 
        // http://localhost/ServiceStack.Examples.Host.Web/Public/Soap12/
        public static string GetParentBaseUrl(this HttpRequestBase request)
        {
            var rawUrl = request.RawUrl; // /Cambia3/Temp/Test.aspx/path/info
            var endpointsPath = rawUrl.Substring(0, rawUrl.LastIndexOf('/') + 1);  // /Cambia3/Temp/Test.aspx/path
            return request.Url.GetLeftAuthority() + endpointsPath;
        }

        public static string GetApplicationUrl(this HttpRequestBase httpReq)
        {
            var appPath = httpReq.ApplicationPath.SanitizedVirtualPath();
            var baseUrl = httpReq.Url.GetLeftAuthority();
            baseUrl = baseUrl.CombineWith(appPath, HostContext.Config.HandlerFactoryPath);
            return baseUrl;
        }

        public static string GetBaseUrl(this HttpRequestBase request)
        {
            return request.Url.GetLeftAuthority() + request.RawUrl;
        }

        public static string GetUrlHostName(this HttpRequestBase request)
        {
            //TODO: Fix bug in mono fastcgi, when trying to get 'Request.Url.Host'
            try
            {
                return request.Url.Host;
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error trying to get 'Request.Url.Host'", ex);

                return request.UserHostName;
            }
        }

        public static string GetLastPathInfo(this HttpRequestBase request)
        {
            return GetLastPathInfoFromRawUrl(request.RawUrl);
        }

        public static string GetOperationName(this HttpRequestBase request)
        {
            var pathInfo = request.GetLastPathInfo();
            return GetOperationNameFromLastPathInfo(pathInfo);
        }

        public static string GetOperationName(this HttpListenerRequest request)
        {
            return request.Url.Segments[request.Url.Segments.Length - 1];
        }

        public static string GetLastPathInfo(this HttpListenerRequest request)
        {
            return GetLastPathInfoFromRawUrl(request.RawUrl);
        }

        public static string GetPathInfo(this HttpRequestBase request)
        {
            if (!IsNullOrEmpty(request.PathInfo)) return request.PathInfo.TrimEnd('/');

            var mode = HostContext.Config.HandlerFactoryPath;
            var appPath = IsNullOrEmpty(request.ApplicationPath)
                          ? WebHostDirectoryName
                          : request.ApplicationPath.TrimStart('/');

            //mod_mono: /CustomPath35/api//default.htm
            var path = Env.IsMono ? request.Path.Replace("//", "/") : request.Path;
            return GetPathInfo(path, mode, appPath);
        }
#endif

        public static string GetPathInfo(string fullPath, string mode, string appPath)
        {
            var pathInfo = ResolvePathInfoFromMappedPath(fullPath, mode);
            if (!IsNullOrEmpty(pathInfo)) return pathInfo;

            //Wildcard mode relies on this to work out the handlerPath
            pathInfo = ResolvePathInfoFromMappedPath(fullPath, appPath);
            if (!IsNullOrEmpty(pathInfo)) return pathInfo;

            return fullPath;
        }

        public static string ResolvePathInfoFromMappedPath(string fullPath, string mappedPathRoot)
        {
            if (mappedPathRoot == null) return null;

            var sbPathInfo = StringBuilderCache.Allocate();
            var fullPathParts = fullPath.Split('/');
            var mappedPathRootParts = mappedPathRoot.Split('/');
            var fullPathIndexOffset = mappedPathRootParts.Length - 1;
            var pathRootFound = false;

            for (var fullPathIndex = 0; fullPathIndex < fullPathParts.Length; fullPathIndex++)
            {
                if (pathRootFound)
                {
                    sbPathInfo.Append("/" + fullPathParts[fullPathIndex]);
                }
                else if (fullPathIndex - fullPathIndexOffset >= 0)
                {
                    pathRootFound = true;
                    for (var mappedPathRootIndex = 0; mappedPathRootIndex < mappedPathRootParts.Length; mappedPathRootIndex++)
                    {
                        if (!string.Equals(fullPathParts[fullPathIndex - fullPathIndexOffset + mappedPathRootIndex], mappedPathRootParts[mappedPathRootIndex], StringComparison.OrdinalIgnoreCase))
                        {
                            pathRootFound = false;
                            break;
                        }
                    }
                }
            }
            if (!pathRootFound) return null;

            var path = StringBuilderCache.ReturnAndFree(sbPathInfo);
            return path.Length > 1 ? path.TrimEnd('/') : "/";
        }

        public static bool IsContentType(this IRequest request, string contentType)
        {
            return request.ContentType.StartsWith(contentType, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasAnyOfContentTypes(this IRequest request, params string[] contentTypes)
        {
            if (contentTypes == null || request.ContentType == null) return false;
            foreach (var contentType in contentTypes)
            {
                if (IsContentType(request, contentType)) return true;
            }
            return false;
        }

        /// <summary>
        /// Duplicate Params are given a unique key by appending a #1 suffix
        /// </summary>
        public static Dictionary<string, string> GetRequestParams(this IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.AllKeys)
            {
                if (name == null) continue; //thank you ASP.NET

                var values = request.QueryString.GetValues(name);
                if (values.Length == 1)
                {
                    map[name] = values[0];
                }
                else
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        map[name + (i == 0 ? "" : "#" + i)] = values[i];
                    }
                }
            }

            if ((request.Verb == HttpMethods.Post || request.Verb == HttpMethods.Put)
                && request.FormData != null)
            {
                foreach (var name in request.FormData.AllKeys)
                {
                    if (name == null) continue; //thank you ASP.NET

                    var values = request.FormData.GetValues(name);
                    if (values.Length == 1)
                    {
                        map[name] = values[0];
                    }
                    else
                    {
                        for (var i = 0; i < values.Length; i++)
                        {
                            map[name + (i == 0 ? "" : "#" + i)] = values[i];
                        }
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Duplicate params have their values joined together in a comma-delimited string
        /// </summary>
        public static Dictionary<string, string> GetFlattenedRequestParams(this IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.AllKeys)
            {
                if (name == null) continue; //thank you ASP.NET
                map[name] = request.QueryString[name];
            }

            if ((request.Verb == HttpMethods.Post || request.Verb == HttpMethods.Put)
                && request.FormData != null)
            {
                foreach (var name in request.FormData.AllKeys)
                {
                    if (name == null) continue; //thank you ASP.NET
                    map[name] = request.FormData[name];
                }
            }

            return map;
        } 

        public static string GetQueryStringContentType(this IRequest httpReq)
        {
            var callback = httpReq.QueryString[Keywords.Callback];
            if (!IsNullOrEmpty(callback)) return MimeTypes.Json;

            var format = httpReq.QueryString[Keywords.Format];
            if (format == null)
            {
                const int formatMaxLength = 4;
                var pi = httpReq.PathInfo;
                if (pi == null || pi.Length <= formatMaxLength) return null;
                if (pi[0] == '/') pi = pi.Substring(1);
                format = pi.LeftPart('/');
                if (format.Length > formatMaxLength) return null;
            }

            format = format.LeftPart('.').ToLower();
            if (format.Contains("json")) return MimeTypes.Json;
            if (format.Contains("xml")) return MimeTypes.Xml;
            if (format.Contains("jsv")) return MimeTypes.Jsv;

            string contentType;
            HostContext.ContentTypes.ContentTypeFormats.TryGetValue(format, out contentType);

            return contentType;
        }

        /// <summary>
        /// Use this to treat Request.Items[] as a cache by returning pre-computed items to save 
        /// calculating them multiple times.
        /// </summary>
        public static object ResolveItem(this IRequest httpReq,
            string itemKey, Func<IRequest, object> resolveFn)
        {
            object cachedItem;
            if (httpReq.Items.TryGetValue(itemKey, out cachedItem))
                return cachedItem;

            var item = resolveFn(httpReq);
            httpReq.Items[itemKey] = item;

            return item;
        }

        public static string GetResponseContentType(this IRequest httpReq)
        {
            var specifiedContentType = GetQueryStringContentType(httpReq);
            if (!IsNullOrEmpty(specifiedContentType)) return specifiedContentType;

            var acceptContentTypes = httpReq.AcceptTypes;
            var defaultContentType = httpReq.ContentType;
            if (httpReq.HasAnyOfContentTypes(MimeTypes.FormUrlEncoded, MimeTypes.MultiPartFormData))
            {
                defaultContentType = HostContext.Config.DefaultContentType;
            }

            var customContentTypes = HostContext.ContentTypes.ContentTypeFormats.Values;
            var preferredContentTypes = HostContext.Config.PreferredContentTypesArray;

            var acceptsAnything = false;
            var hasDefaultContentType = !IsNullOrEmpty(defaultContentType);
            if (acceptContentTypes != null)
            {
                var hasPreferredContentTypes = new bool[preferredContentTypes.Length];
                foreach (var acceptsType in acceptContentTypes)
                {
                    var contentType = ContentFormat.GetRealContentType(acceptsType);
                    acceptsAnything = acceptsAnything || contentType == "*/*";

                    for (var i = 0; i < preferredContentTypes.Length; i++)
                    {
                        if (hasPreferredContentTypes[i]) continue;
                        var preferredContentType = preferredContentTypes[i];
                        hasPreferredContentTypes[i] = contentType.StartsWith(preferredContentType);

                        //Prefer Request.ContentType if it is also a preferredContentType
                        if (hasPreferredContentTypes[i] && preferredContentType == defaultContentType)
                            return preferredContentType;
                    }
                }

                for (var i = 0; i < preferredContentTypes.Length; i++)
                {
                    if (hasPreferredContentTypes[i]) return preferredContentTypes[i];
                }

                if (acceptsAnything)
                {
                    if (hasDefaultContentType)
                        return defaultContentType;
                    if (HostContext.Config.DefaultContentType != null)
                        return HostContext.Config.DefaultContentType;
                }

                foreach (var contentType in acceptContentTypes)
                {
                    foreach (var customContentType in customContentTypes)
                    {
                        if (contentType.StartsWith(customContentType, StringComparison.OrdinalIgnoreCase))
                            return customContentType;
                    }
                }
            }

            if (httpReq.ContentType.MatchesContentType(MimeTypes.Soap12))
            {
                return MimeTypes.Soap12;
            }

            if (acceptContentTypes == null && httpReq.ContentType == MimeTypes.Soap11)
            {
                return MimeTypes.Soap11;
            }

            //We could also send a '406 Not Acceptable', but this is allowed also
            return HostContext.Config.DefaultContentType;
        }

        public static void SetView(this IRequest httpReq, string viewName)
        {
            httpReq.SetItem("View", viewName);
        }

        public static string GetView(this IRequest httpReq)
        {
            return httpReq.GetItem("View") as string;
        }

        public static void SetTemplate(this IRequest httpReq, string templateName)
        {
            httpReq.SetItem("Template", templateName);
        }

        public static string GetTemplate(this IRequest httpReq)
        {
            return httpReq.GetItem("Template") as string;
        }

        public static string ResolveAbsoluteUrl(this IRequest httpReq, string url)
        {
            return HostContext.ResolveAbsoluteUrl(url, httpReq);
        }

        public static string GetAbsoluteUrl(this IRequest httpReq, string url)
        {
            if (url?.SafeSubstring(0, 2) == "~/")
            {
                url = httpReq.GetBaseUrl().CombineWith(url.Substring(2));
            }
            return url;
        }

        public static string InferBaseUrl(this string absoluteUri, string fromPathInfo = null)
        {
            if (IsNullOrEmpty(fromPathInfo))
            {
                fromPathInfo = "/" + (HostContext.Config.HandlerFactoryPath ?? "");
            }
            else
            {
                fromPathInfo = fromPathInfo.TrimEnd('/');
                if (fromPathInfo.Length == 0)
                    return null;
            }

            if (IsNullOrEmpty(absoluteUri))
                return null;

            var pos = absoluteUri.IndexOf(fromPathInfo, "https://".Length + 1, StringComparison.Ordinal);
            return pos >= 0 ? absoluteUri.Substring(0, pos) : absoluteUri;
        }

        public static string GetBaseUrl(this IRequest httpReq)
        {
            return HostContext.AppHost.GetBaseUrl(httpReq);
        }

        public static bool UseHttps(this IRequest httpReq)
        {
            return HostContext.Config.UseHttpsLinks ||
                httpReq.GetHeader(HttpHeaders.XForwardedProtocol) == "https";
        }

        public static string NormalizeScheme(this string url, bool useHttps)
        {
            if (url == null || !useHttps)
                return url;

            url = url.TrimStart();
            if (url.StartsWith("http://"))
                return "https://" + url.Substring("http://".Length);

            return url;
        }

        public static RequestAttributes ToRequestAttributes(string[] attrNames)
        {
            var attrs = RequestAttributes.None;
            foreach (var simulatedAttr in attrNames)
            {
                var attr = (RequestAttributes)Enum.Parse(typeof(RequestAttributes), simulatedAttr, true);
                attrs |= attr;
            }
            return attrs;
        }

        public static RequestAttributes GetAttributes(this IRequest request)
        {
            if (HostContext.DebugMode
                && request.QueryString != null) //Mock<IHttpRequest>
            {
                var simulate = request.QueryString["simulate"];
                if (simulate != null)
                {
                    return ToRequestAttributes(simulate.Split(','));
                }
            }

            var portRestrictions = RequestAttributes.None;

            portRestrictions |= ContentFormat.GetRequestAttribute(request.Verb);
            portRestrictions |= request.IsSecureConnection ? RequestAttributes.Secure : RequestAttributes.InSecure;

            if (request.UserHostAddress != null)
            {
                var isIpv4Address = request.UserHostAddress.IndexOf('.') != -1
                    && request.UserHostAddress.IndexOf("::", StringComparison.Ordinal) == -1;

                string ipAddressNumber = null;
                if (isIpv4Address)
                {
                    ipAddressNumber = request.UserHostAddress.LeftPart(":");
                }
                else
                {
                    if (request.UserHostAddress.Contains("]:"))
                    {
                        ipAddressNumber = request.UserHostAddress.LastLeftPart(":");
                    }
                    else
                    {
                        ipAddressNumber = request.UserHostAddress.LastIndexOf("%", StringComparison.Ordinal) > 0 ?
                            request.UserHostAddress.LastLeftPart(":") :
                            request.UserHostAddress;
                    }
                }

                try
                {
                    ipAddressNumber = ipAddressNumber.LeftPart(',');
                    var ipAddress = ipAddressNumber.StartsWith("::1")
                        ? IPAddress.IPv6Loopback
                        : IPAddress.Parse(ipAddressNumber);
                    portRestrictions |= GetAttributes(ipAddress);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Could not parse Ipv{(isIpv4Address ? 4 : 6)} Address: {request.UserHostAddress} / {ipAddressNumber}", ex);
                }
            }

            return portRestrictions;
        }

        public static RequestAttributes GetAttributes(IPAddress ipAddress)
        {
            if (IPAddress.IsLoopback(ipAddress))
                return RequestAttributes.Localhost;

            return IsInLocalSubnet(ipAddress)
                   ? RequestAttributes.LocalSubnet
                   : RequestAttributes.External;
        }

        public static bool IsInLocalSubnet(IPAddress ipAddress)
        {
            var ipAddressBytes = ipAddress.GetAddressBytes();
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    foreach (var localIpv4AddressAndMask in ServiceStackHandlerBase.NetworkInterfaceIpv4Addresses)
                    {
                        if (ipAddressBytes.IsInSameIpv4Subnet(localIpv4AddressAndMask.Key, localIpv4AddressAndMask.Value))
                        {
                            return true;
                        }
                    }
                    break;

                case AddressFamily.InterNetworkV6:
                    foreach (var localIpv6Address in ServiceStackHandlerBase.NetworkInterfaceIpv6Addresses)
                    {
                        if (ipAddressBytes.IsInSameIpv6Subnet(localIpv6Address))
                        {
                            return true;
                        }
                    }
                    break;
            }

            return false;
        }

#if !NETSTANDARD1_6
        public static HttpContextBase ToHttpContextBase(this HttpRequestBase aspnetHttpReq)
        {
            return aspnetHttpReq.RequestContext.HttpContext;
        }

        public static HttpContextBase ToHttpContextBase(this HttpContext httpContext)
        {
            return httpContext.Request.RequestContext.HttpContext;
        }

        public static IHttpRequest ToRequest(this HttpContext httpCtx, string operationName = null)
        {
            if (httpCtx == null)
                throw new NotImplementedException(ErrorMessages.OnlyAllowedInAspNetHosts);

            return new AspNetRequest(httpCtx.ToHttpContextBase(), operationName);
        }

        public static IHttpRequest ToRequest(this HttpContextBase httpCtx, string operationName = null)
        {
            return new AspNetRequest(httpCtx, operationName);
        }

        public static IHttpRequest ToRequest(this HttpRequestBase httpCtx, string operationName = null)
        {
            return new AspNetRequest(httpCtx.ToHttpContextBase(), operationName);
        }

        public static IHttpRequest ToRequest(this HttpListenerContext httpCtxReq, string operationName = null)
        {
            return ((HttpListenerBase)ServiceStackHost.Instance).CreateRequest(httpCtxReq, operationName);
        }

        public static IHttpResponse ToResponse(this HttpContext httpCtx)
        {
            return httpCtx.ToRequest().HttpResponse;
        }

        public static IHttpResponse ToResponse(this HttpRequestBase aspReq)
        {
            return aspReq.ToRequest().HttpResponse;
        }

        public static IHttpResponse ToResponse(this HttpListenerContext httpCtx)
        {
            return httpCtx.ToRequest().HttpResponse;
        }

        public static System.ServiceModel.Channels.Message GetSoapMessage(this IRequest httpReq)
        {
            return httpReq.Items[Keywords.SoapMessage] as System.ServiceModel.Channels.Message;
        }

        public static string GetPathAndQuery(this HttpRequestBase request)
        {
            return request != null && request.Url != null
                ? request.Url.PathAndQuery
                : null;
        }
#else
        public static string GetPathAndQuery(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            if (request == null)
                return null;

            var url = new Uri(Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request));
            return url.PathAndQuery;
        }
#endif

        public static void SetOperationName(this IRequest httpReq, string operationName)
        {
            if (httpReq.OperationName == null)
            {
#if !NETSTANDARD1_6
                var aspReq = httpReq as AspNetRequest;
                if (aspReq != null)
                {
                    aspReq.OperationName = operationName;
                    return;
                }

                var listenerReq = httpReq as ListenerRequest;
                if (listenerReq != null)
                {
                    listenerReq.OperationName = operationName;
                }
#endif
            }
        }

        public static Type GetOperationType(this IRequest req)
        {
            if (req.Dto != null)
            {
                var dtoType = req.Dto.GetType();
                return dtoType.IsArray
                    ? dtoType.GetElementType()
                    : dtoType;
            }
            return req.OperationName != null
                ? HostContext.Metadata.GetOperationType(req.OperationName)
                : null;
        }

        public static bool IsMultiRequest(this IRequest req)
        {
            //Only way to send T[] is via /reply/operation[] predefined route
            return req.Dto != null && req.Dto.GetType().IsArray;
        }

        public static void SetAutoBatchCompletedHeader(this IRequest req, int completed)
        {
            if (req == null || req.Response == null)
                return;

            req.Response.AddHeader(HttpHeaders.XAutoBatchCompleted, completed.ToString());
        }

        public static void SetRoute(this IRequest req, RestPath route)
        {
            req.Items[Keywords.Route] = route;
        }

        public static RestPath GetRoute(this IRequest req)
        {
            object route;
            req.Items.TryGetValue(Keywords.Route, out route);
            return route as RestPath;
        }
    }
}