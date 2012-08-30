using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
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
	public static class HttpRequestExtensions
	{
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
			if (string.IsNullOrEmpty(lastPathInfo)) return null;

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
			if (string.IsNullOrEmpty(pathInfo))
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
			if (!string.IsNullOrEmpty(request.PathInfo)) return request.PathInfo.TrimEnd('/');

			var mode = EndpointHost.Config.ServiceStackHandlerFactoryPath;
			var appPath = string.IsNullOrEmpty(request.ApplicationPath)
			              ? WebHostDirectoryName
			              : request.ApplicationPath.TrimStart('/');

			//mod_mono: /CustomPath35/api//default.htm
			var path = Env.IsMono ? request.Path.Replace("//", "/") : request.Path;
			return GetPathInfo(path, mode, appPath);
		}

		public static string GetPathInfo(string fullPath, string mode, string appPath)
		{
			var pathInfo = ResolvePathInfoFromMappedPath(fullPath, mode);
			if (!string.IsNullOrEmpty(pathInfo)) return pathInfo;
			
			//Wildcard mode relies on this to find work out the handlerPath
			pathInfo = ResolvePathInfoFromMappedPath(fullPath, appPath);
			if (!string.IsNullOrEmpty(pathInfo)) return pathInfo;
			
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

            for (var fullPathIndex = 0; fullPathIndex < fullPathParts.Length; fullPathIndex++) {
                if (pathRootFound) {
                    sbPathInfo.Append("/" + fullPathParts[fullPathIndex]);
                } else if (fullPathIndex - fullPathIndexOffset >= 0) {
                    pathRootFound = true;
                    for (var mappedPathRootIndex = 0; mappedPathRootIndex < mappedPathRootParts.Length; mappedPathRootIndex++) {
                        if (!string.Equals(fullPathParts[fullPathIndex - fullPathIndexOffset + mappedPathRootIndex], mappedPathRootParts[mappedPathRootIndex], StringComparison.InvariantCultureIgnoreCase)) {
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
			if (!string.IsNullOrEmpty(callback)) return ContentType.Json;

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
			if (format.Contains("json")) return ContentType.Json;
			if (format.Contains("xml")) return ContentType.Xml;
			if (format.Contains("jsv")) return ContentType.Jsv;

			string contentType;
			EndpointHost.ContentTypeFilter.ContentTypeFormats.TryGetValue(format, out contentType);

			return contentType;
		}

		public static string[] PreferredContentTypes = new[] {
			ContentType.Html, ContentType.Json, ContentType.Xml, ContentType.Jsv
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
			if (!string.IsNullOrEmpty(specifiedContentType)) return specifiedContentType;

			var acceptContentTypes = httpReq.AcceptTypes;
			var defaultContentType = httpReq.ContentType;
			if (httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData))
			{
				defaultContentType = EndpointHost.Config.DefaultContentType;
			}

			var customContentTypes = EndpointHost.ContentTypeFilter.ContentTypeFormats.Values;

			var acceptsAnything = false;
			var hasDefaultContentType = !string.IsNullOrEmpty(defaultContentType);
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


        /// <summary>
        /// Store an entry in the IHttpRequest.Items Dictionary
        /// </summary>
        public static void SetItem(this IHttpRequest httpReq, string key, object value)
        {
            if (httpReq != null)
                httpReq.Items[key] = value;
        }

        /// <summary>
        /// Get an entry from the IHttpRequest.Items Dictionary
        /// </summary>
        public static object GetItem(this IHttpRequest httpReq, string key)
        {
            object value = null;
            if (httpReq != null)
                httpReq.Items.TryGetValue(key, out value);
            return value;
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
    }


}