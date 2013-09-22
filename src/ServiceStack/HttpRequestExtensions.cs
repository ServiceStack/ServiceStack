using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.Utils;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Wrappers;
using HttpRequestWrapper = ServiceStack.WebHost.Endpoints.Wrappers.HttpRequestWrapper;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Wrappers.HttpResponseWrapper;

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
		public static string GetItemOrCookie(this IHttpRequest httpReq, string name)
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
		public static string GetParam(this IHttpRequest httpReq, string name)
		{
			string value;
			if ((value = httpReq.Headers[HttpHeaders.XParamOverridePrefix + name]) != null) return value;
			if ((value = httpReq.QueryString[name]) != null) return value;
			if ((value = httpReq.FormData[name]) != null) return value;

            //IIS will assign null to params without a name: .../?some_value can be retrieved as req.Params[null]
            //TryGetValue is not happy with null dictionary keys, so we should bail out here
            if (string.IsNullOrEmpty(name)) return null;

			Cookie cookie;
			if (httpReq.Cookies.TryGetValue(name, out cookie)) return cookie.Value;

			object oValue;
			if (httpReq.Items.TryGetValue(name, out oValue)) return oValue.ToString();

			return null;
		}

		public static string GetParentAbsolutePath(this IHttpRequest httpReq)
		{
			return httpReq.GetAbsolutePath().ToParentPath();
		}

		public static string GetAbsolutePath(this IHttpRequest httpReq)
		{
			var resolvedPathInfo = httpReq.PathInfo;

			var pos = httpReq.RawUrl.IndexOf(resolvedPathInfo, StringComparison.InvariantCultureIgnoreCase);
			if (pos == -1)
				throw new ArgumentException(
					String.Format("PathInfo '{0}' is not in Url '{1}'", resolvedPathInfo, httpReq.RawUrl));

			return httpReq.RawUrl.Substring(0, pos + resolvedPathInfo.Length);
		}

		public static string GetParentPathUrl(this IHttpRequest httpReq)
		{
			return httpReq.GetPathUrl().ToParentPath();
		}
		
		public static string GetPathUrl(this IHttpRequest httpReq)
		{
			var resolvedPathInfo = httpReq.PathInfo;

			var pos = resolvedPathInfo == String.Empty
				? httpReq.AbsoluteUri.Length
				: httpReq.AbsoluteUri.IndexOf(resolvedPathInfo, StringComparison.InvariantCultureIgnoreCase);

			if (pos == -1)
				throw new ArgumentException(
					String.Format("PathInfo '{0}' is not in Url '{1}'", resolvedPathInfo, httpReq.RawUrl));

			return httpReq.AbsoluteUri.Substring(0, pos + resolvedPathInfo.Length);
		}

		public static string GetUrlHostName(this IHttpRequest httpReq)
		{
			var aspNetReq = httpReq as HttpRequestWrapper;
			if (aspNetReq != null)
			{
				return aspNetReq.UrlHostName;
			}
			var uri = httpReq.AbsoluteUri;

			var pos = uri.IndexOf("://") + "://".Length;
			var partialUrl = uri.Substring(pos);
			var endPos = partialUrl.IndexOf('/');
			if (endPos == -1) endPos = partialUrl.Length;
			var hostName = partialUrl.Substring(0, endPos).Split(':')[0];
			return hostName;
		}

		public static string GetPhysicalPath(this IHttpRequest httpReq)
		{
		    var aspNetReq = httpReq as HttpRequestWrapper;
			var res = aspNetReq != null 
                ? aspNetReq.Request.PhysicalPath 
                : EndpointHostConfig.Instance.WebHostPhysicalPath.CombineWith(httpReq.PathInfo);

			return res;
		}

        public static string GetApplicationUrl(this HttpRequest httpReq)
        {
            var appPath = httpReq.ApplicationPath;
            var baseUrl = httpReq.Url.Scheme + "://" + httpReq.Url.Host;
            if (httpReq.Url.Port != 80) baseUrl += ":" + httpReq.Url.Port;
            var appUrl = baseUrl.CombineWith(appPath);
            return appUrl;
        }

        public static string GetApplicationUrl(this IHttpRequest httpReq)
        {
            var url = new Uri(httpReq.AbsoluteUri);
            var baseUrl = url.Scheme + "://" + url.Host;
            if (url.Port != 80) baseUrl += ":" + url.Port;
            var appUrl = baseUrl.CombineWith(EndpointHost.Config.ServiceStackHandlerFactoryPath);
            return appUrl;
        }

		public static string GetHttpMethodOverride(this IHttpRequest httpReq)
		{
			var httpMethod = httpReq.HttpMethod;

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

		public static string GetFormatModifier(this IHttpRequest httpReq)
		{
			var format = httpReq.QueryString["format"];
			if (format == null) return null;
			var parts = format.SplitOnFirst('.');
			return parts.Length > 1 ? parts[1] : null;
		}

		public static bool HasNotModifiedSince(this IHttpRequest httpReq, DateTime? dateTime)
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

		public static bool DidReturn304NotModified(this IHttpRequest httpReq, DateTime? dateTime, IHttpResponse httpRes)
		{
			if (httpReq.HasNotModifiedSince(dateTime))
			{
				httpRes.StatusCode = (int) HttpStatusCode.NotModified;
				return true;
			}
			return false;
		}

		public static string GetJsonpCallback(this IHttpRequest httpReq)
		{
			return httpReq == null ? null : httpReq.QueryString["callback"];
		}


		public static Dictionary<string, string> CookiesAsDictionary(this IHttpRequest httpReq)
		{
			var map = new Dictionary<string, string>();
			var aspNet = httpReq.OriginalRequest as HttpRequest;
			if (aspNet != null)
			{
				foreach (var name in aspNet.Cookies.AllKeys)
				{
					var cookie = aspNet.Cookies[name];
					if (cookie == null) continue;
					map[name] = cookie.Value;
				}
			}
			else
			{
				var httpListener = httpReq.OriginalRequest as HttpListenerRequest;
				if (httpListener != null)
				{
					for (var i = 0; i < httpListener.Cookies.Count; i++)
					{
						var cookie = httpListener.Cookies[i];
						if (cookie == null || cookie.Name == null) continue;
						map[cookie.Name] = cookie.Value;
					}
				}
			}
			return map;
		}

        public static int ToStatusCode(this Exception ex)
        {
            int errorStatus;
            if (EndpointHost.Config != null && EndpointHost.Config.MapExceptionToStatusCode.TryGetValue(ex.GetType(), out errorStatus))
            {
                return errorStatus;
            }

            if (ex is HttpError) return ((HttpError)ex).Status;
            if (ex is NotImplementedException || ex is NotSupportedException) return (int)HttpStatusCode.MethodNotAllowed;
            if (ex is ArgumentException || ex is SerializationException) return (int)HttpStatusCode.BadRequest;
            if (ex is UnauthorizedAccessException) return (int) HttpStatusCode.Forbidden;
            return (int)HttpStatusCode.InternalServerError;
	    }
        
        public static string ToErrorCode(this Exception ex)
        {
            if (ex is HttpError) return ((HttpError)ex).ErrorCode;
            return ex.GetType().Name;
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

        private static string WebHostDirectoryName = "";

        static HttpRequestExtensions()
        {
            WebHostDirectoryName = Path.GetFileName("~".MapHostAbsolutePath());
        }

        public static string GetOperationName(this HttpRequest request)
        {
            var pathInfo = request.GetLastPathInfo();
            return GetOperationNameFromLastPathInfo(pathInfo);
        }

        public static string GetOperationNameFromLastPathInfo(string lastPathInfo)
        {
            if (String.IsNullOrEmpty(lastPathInfo)) return null;

            var operationName = lastPathInfo.Substring("/".Length);

            return operationName;
        }

        private static string GetLastPathInfoFromRawUrl(string rawUrl)
        {
            var pathInfo = rawUrl.IndexOf("?") != -1
                ? rawUrl.Substring(0, rawUrl.IndexOf("?"))
                : rawUrl;

            pathInfo = pathInfo.Substring(pathInfo.LastIndexOf("/"));

            return pathInfo;
        }

        public static string GetLastPathInfo(this HttpRequest request)
        {
            var pathInfo = request.PathInfo;
            if (String.IsNullOrEmpty(pathInfo))
            {
                pathInfo = GetLastPathInfoFromRawUrl(request.RawUrl);
            }

            //Log.DebugFormat("Request.PathInfo: {0}, Request.RawUrl: {1}, pathInfo:{2}",
            //    request.PathInfo, request.RawUrl, pathInfo);

            return pathInfo;
        }

        public static string GetUrlHostName(this HttpRequest request)
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

        // http://localhost/ServiceStack.Examples.Host.Web/Public/Public/Soap12/Wsdl => 
        // http://localhost/ServiceStack.Examples.Host.Web/Public/Soap12/
        public static string GetParentBaseUrl(this HttpRequest request)
        {
            var rawUrl = request.RawUrl; // /Cambia3/Temp/Test.aspx/path/info
            var endpointsPath = rawUrl.Substring(0, rawUrl.LastIndexOf('/') + 1);  // /Cambia3/Temp/Test.aspx/path
            return GetAuthority(request) + endpointsPath;
        }

        public static string GetParentBaseUrl(this IHttpRequest request)
        {
            var rawUrl = request.RawUrl;
            var endpointsPath = rawUrl.Substring(0, rawUrl.LastIndexOf('/') + 1);
            return new Uri(request.AbsoluteUri).GetLeftPart(UriPartial.Authority) + endpointsPath;
        }

        public static string GetBaseUrl(this HttpRequest request)
        {
            return GetAuthority(request) + request.RawUrl;
        }

        //=> http://localhost:96 ?? ex=> http://localhost
        private static string GetAuthority(HttpRequest request)
        {
            try
            {
                return request.Url.GetLeftPart(UriPartial.Authority);
            }
            catch (Exception ex)
            {
                Log.Error("Error trying to get: request.Url.GetLeftPart(UriPartial.Authority): " + ex.Message, ex);
                return "http://" + request.UserHostName;
            }
        }

        public static string GetOperationName(this HttpListenerRequest request)
        {
            return request.Url.Segments[request.Url.Segments.Length - 1];
        }

        public static string GetLastPathInfo(this HttpListenerRequest request)
        {
            return GetLastPathInfoFromRawUrl(request.RawUrl);
        }

        public static string GetPathInfo(this HttpRequest request)
        {
            if (!String.IsNullOrEmpty(request.PathInfo)) return request.PathInfo.TrimEnd('/');

            var mode = EndpointHost.Config.ServiceStackHandlerFactoryPath;
            var appPath = String.IsNullOrEmpty(request.ApplicationPath)
                          ? WebHostDirectoryName
                          : request.ApplicationPath.TrimStart('/');

            //mod_mono: /CustomPath35/api//default.htm
            var path = Env.IsMono ? request.Path.Replace("//", "/") : request.Path;
            return GetPathInfo(path, mode, appPath);
        }

        public static string GetPathInfo(string fullPath, string mode, string appPath)
        {
            var pathInfo = ResolvePathInfoFromMappedPath(fullPath, mode);
            if (!String.IsNullOrEmpty(pathInfo)) return pathInfo;

            //Wildcard mode relies on this to work out the handlerPath
            pathInfo = ResolvePathInfoFromMappedPath(fullPath, appPath);
            if (!String.IsNullOrEmpty(pathInfo)) return pathInfo;

            return fullPath;
        }

        public static string ResolvePathInfoFromMappedPath(string fullPath, string mappedPathRoot)
        {
            if (mappedPathRoot == null) return null;

            var sbPathInfo = new StringBuilder();
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
                        if (!String.Equals(fullPathParts[fullPathIndex - fullPathIndexOffset + mappedPathRootIndex], mappedPathRootParts[mappedPathRootIndex], StringComparison.InvariantCultureIgnoreCase))
                        {
                            pathRootFound = false;
                            break;
                        }
                    }
                }
            }
            if (!pathRootFound) return null;

            var path = sbPathInfo.ToString();
            return path.Length > 1 ? path.TrimEnd('/') : "/";
        }

        public static bool IsContentType(this IHttpRequest request, string contentType)
        {
            return request.ContentType.StartsWith(contentType, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool HasAnyOfContentTypes(this IHttpRequest request, params string[] contentTypes)
        {
            if (contentTypes == null || request.ContentType == null) return false;
            foreach (var contentType in contentTypes)
            {
                if (IsContentType(request, contentType)) return true;
            }
            return false;
        }

        public static IHttpRequest GetHttpRequest(this HttpRequest request)
        {
            return new HttpRequestWrapper(null, request);
        }

        public static IHttpRequest GetHttpRequest(this HttpListenerRequest request)
        {
            return new HttpListenerRequestWrapper(null, request);
        }

        public static Dictionary<string, string> GetRequestParams(this IHttpRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.AllKeys)
            {
                if (name == null) continue; //thank you ASP.NET
                map[name] = request.QueryString[name];
            }

            if ((request.HttpMethod == HttpMethods.Post || request.HttpMethod == HttpMethods.Put)
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

        public static string GetQueryStringContentType(this IHttpRequest httpReq)
        {
            var callback = httpReq.QueryString["callback"];
            if (!String.IsNullOrEmpty(callback)) return MimeTypes.Json;

            var format = httpReq.QueryString["format"];
            if (format == null)
            {
                const int formatMaxLength = 4;
                var pi = httpReq.PathInfo;
                if (pi == null || pi.Length <= formatMaxLength) return null;
                if (pi[0] == '/') pi = pi.Substring(1);
                format = pi.SplitOnFirst('/')[0];
                if (format.Length > formatMaxLength) return null;
            }

            format = format.SplitOnFirst('.')[0].ToLower();
            if (format.Contains("json")) return MimeTypes.Json;
            if (format.Contains("xml")) return MimeTypes.Xml;
            if (format.Contains("jsv")) return MimeTypes.Jsv;

            string contentType;
            EndpointHost.ContentTypes.ContentTypeFormats.TryGetValue(format, out contentType);

            return contentType;
        }

        public static string[] PreferredContentTypes = new[] {
			MimeTypes.Html, MimeTypes.Json, MimeTypes.Xml, MimeTypes.Jsv
		};

        /// <summary>
        /// Use this to treat Request.Items[] as a cache by returning pre-computed items to save 
        /// calculating them multiple times.
        /// </summary>
        public static object ResolveItem(this IHttpRequest httpReq,
            string itemKey, Func<IHttpRequest, object> resolveFn)
        {
            object cachedItem;
            if (httpReq.Items.TryGetValue(itemKey, out cachedItem))
                return cachedItem;

            var item = resolveFn(httpReq);
            httpReq.Items[itemKey] = item;

            return item;
        }

        public static string GetResponseContentType(this IHttpRequest httpReq)
        {
            var specifiedContentType = GetQueryStringContentType(httpReq);
            if (!String.IsNullOrEmpty(specifiedContentType)) return specifiedContentType;

            var acceptContentTypes = httpReq.AcceptTypes;
            var defaultContentType = httpReq.ContentType;
            if (httpReq.HasAnyOfContentTypes(MimeTypes.FormUrlEncoded, MimeTypes.MultiPartFormData))
            {
                defaultContentType = EndpointHost.Config.DefaultContentType;
            }

            var customContentTypes = EndpointHost.ContentTypes.ContentTypeFormats.Values;

            var acceptsAnything = false;
            var hasDefaultContentType = !String.IsNullOrEmpty(defaultContentType);
            if (acceptContentTypes != null)
            {
                var hasPreferredContentTypes = new bool[PreferredContentTypes.Length];
                foreach (var contentType in acceptContentTypes)
                {
                    acceptsAnything = acceptsAnything || contentType == "*/*";

                    for (var i = 0; i < PreferredContentTypes.Length; i++)
                    {
                        if (hasPreferredContentTypes[i]) continue;
                        var preferredContentType = PreferredContentTypes[i];
                        hasPreferredContentTypes[i] = contentType.StartsWith(preferredContentType);

                        //Prefer Request.ContentType if it is also a preferredContentType
                        if (hasPreferredContentTypes[i] && preferredContentType == defaultContentType)
                            return preferredContentType;
                    }
                }
                for (var i = 0; i < PreferredContentTypes.Length; i++)
                {
                    if (hasPreferredContentTypes[i]) return PreferredContentTypes[i];
                }
                if (acceptsAnything && hasDefaultContentType) return defaultContentType;

                foreach (var contentType in acceptContentTypes)
                {
                    foreach (var customContentType in customContentTypes)
                    {
                        if (contentType.StartsWith(customContentType)) return customContentType;
                    }
                }
            }

            //We could also send a '406 Not Acceptable', but this is allowed also
            return EndpointHost.Config.DefaultContentType;
        }

        public static void SetView(this IHttpRequest httpReq, string viewName)
        {
            httpReq.SetItem("View", viewName);
        }

        public static string GetView(this IHttpRequest httpReq)
        {
            return httpReq.GetItem("View") as string;
        }

        public static void SetTemplate(this IHttpRequest httpReq, string templateName)
        {
            httpReq.SetItem("Template", templateName);
        }

        public static string GetTemplate(this IHttpRequest httpReq)
        {
            return httpReq.GetItem("Template") as string;
        }

        public static string ResolveAbsoluteUrl(this IHttpRequest httpReq, string url)
        {
            return EndpointHost.AppHost.ResolveAbsoluteUrl(url, httpReq);
        }

        public static string ResolveBaseUrl(this IHttpRequest httpReq)
        {
            return EndpointHost.AppHost.ResolveAbsoluteUrl("~/", httpReq);
        }

        public static string GetAbsoluteUrl(this IHttpRequest httpReq, string url)
        {
            if (url.SafeSubstring(0, 2) == "~/")
            {
                url = httpReq.GetBaseUrl().CombineWith(url.Substring(2));
            }
            return url;
        }

        public static string GetBaseUrl(this IHttpRequest httpReq)
        {
            var baseUrl = ServiceStackHttpHandlerFactory.GetBaseUrl();
            if (baseUrl != null) return baseUrl;

            var handlerPath = EndpointHost.Config.ServiceStackHandlerFactoryPath;
            if (handlerPath != null)
            {
                var pos = httpReq.AbsoluteUri.IndexOf(handlerPath, StringComparison.InvariantCultureIgnoreCase);
                if (pos >= 0)
                {
                    baseUrl = httpReq.AbsoluteUri.Substring(0, pos + handlerPath.Length);
                    return baseUrl;
                }
                return "/" + handlerPath;
            }

            return "/"; //Can't infer Absolute Uri, fallback to root relative path
        }

        public static EndpointAttributes ToEndpointAttributes(string[] attrNames)
        {
            var attrs = EndpointAttributes.None;
            foreach (var simulatedAttr in attrNames)
            {
                var attr = (EndpointAttributes)Enum.Parse(typeof(EndpointAttributes), simulatedAttr, true);
                attrs |= attr;
            }
            return attrs;
        }

        public static EndpointAttributes GetAttributes(this IHttpRequest request)
        {
            if (EndpointHost.DebugMode
                && request.QueryString != null) //Mock<IHttpRequest>
            {
                var simulate = request.QueryString["simulate"];
                if (simulate != null)
                {
                    return ToEndpointAttributes(simulate.Split(','));
                }
            }

            var portRestrictions = EndpointAttributes.None;

            portRestrictions |= HttpMethods.GetEndpointAttribute(request.HttpMethod);
            portRestrictions |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;

            if (request.UserHostAddress != null)
            {
                var isIpv4Address = request.UserHostAddress.IndexOf('.') != -1
                    && request.UserHostAddress.IndexOf("::", StringComparison.InvariantCulture) == -1;

                string ipAddressNumber = null;
                if (isIpv4Address)
                {
                    ipAddressNumber = request.UserHostAddress.SplitOnFirst(":")[0];
                }
                else
                {
                    if (request.UserHostAddress.Contains("]:"))
                    {
                        ipAddressNumber = request.UserHostAddress.SplitOnLast(":")[0];
                    }
                    else
                    {
                        ipAddressNumber = request.UserHostAddress.LastIndexOf("%", StringComparison.InvariantCulture) > 0 ?
                            request.UserHostAddress.SplitOnLast(":")[0] :
                            request.UserHostAddress;
                    }
                }

                try
                {
                    ipAddressNumber = ipAddressNumber.SplitOnFirst(',')[0];
                    var ipAddress = ipAddressNumber.StartsWith("::1")
                        ? IPAddress.IPv6Loopback
                        : IPAddress.Parse(ipAddressNumber);
                    portRestrictions |= GetAttributes(ipAddress);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Could not parse Ipv{0} Address: {1} / {2}"
                        .Fmt((isIpv4Address ? 4 : 6), request.UserHostAddress, ipAddressNumber), ex);
                }
            }

            return portRestrictions;
        }

        public static EndpointAttributes GetAttributes(IPAddress ipAddress)
        {
            if (IPAddress.IsLoopback(ipAddress))
                return EndpointAttributes.Localhost;

            return IsInLocalSubnet(ipAddress)
                   ? EndpointAttributes.LocalSubnet
                   : EndpointAttributes.External;
        }

        public static bool IsInLocalSubnet(IPAddress ipAddress)
        {
            var ipAddressBytes = ipAddress.GetAddressBytes();
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    foreach (var localIpv4AddressAndMask in EndpointHandlerBase.NetworkInterfaceIpv4Addresses)
                    {
                        if (ipAddressBytes.IsInSameIpv4Subnet(localIpv4AddressAndMask.Key, localIpv4AddressAndMask.Value))
                        {
                            return true;
                        }
                    }
                    break;

                case AddressFamily.InterNetworkV6:
                    foreach (var localIpv6Address in EndpointHandlerBase.NetworkInterfaceIpv6Addresses)
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

        public static IHttpRequest ToRequest(this HttpRequest aspnetHttpReq, string operationName = null)
        {
            return new HttpRequestWrapper(aspnetHttpReq)
            {
                OperationName = operationName,
                Container = AppHostBase.Instance != null ? AppHostBase.Instance.Container : null
            };
        }

        public static IHttpRequest ToRequest(this HttpListenerRequest listenerHttpReq, string operationName = null)
        {
            return new HttpListenerRequestWrapper(listenerHttpReq)
            {
                OperationName = operationName,
                Container = AppHostBase.Instance != null ? AppHostBase.Instance.Container : null
            };
        }

        public static IHttpResponse ToResponse(this HttpResponse aspnetHttpRes)
        {
            return new HttpResponseWrapper(aspnetHttpRes);
        }

        public static IHttpResponse ToResponse(this HttpListenerResponse listenerHttpRes)
        {
            return new HttpListenerResponseWrapper(listenerHttpRes);
        }

        public static void SetOperationName(this IHttpRequest httpReq, string operationName)
        {
            if (httpReq.OperationName == null)
            {
                var aspReq = httpReq as HttpRequestWrapper;
                if (aspReq != null)
                {
                    aspReq.OperationName = operationName;
                    return;
                }

                var listenerReq = httpReq as HttpListenerRequestWrapper;
                if (listenerReq != null)
                {
                    listenerReq.OperationName = operationName;
                }
            }
        }

        public static System.ServiceModel.Channels.Message GetSoapMessage(this IHttpRequest httpReq)
        {
            return httpReq.Items["SoapMessage"] as System.ServiceModel.Channels.Message;
        }
    }
}