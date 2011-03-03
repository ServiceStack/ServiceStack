using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceHost
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
			if ((value = httpReq.QueryString[name]) != null) return value;
			if ((value = httpReq.FormData[name]) != null) return value;

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
					string.Format("PathInfo '{0}' is not in Url '{1}'", resolvedPathInfo, httpReq.RawUrl));

			return httpReq.RawUrl.Substring(0, pos + resolvedPathInfo.Length);
		}

		public static string GetParentPathUrl(this IHttpRequest httpReq)
		{
			return httpReq.GetPathUrl().ToParentPath();
		}
		
		public static string GetPathUrl(this IHttpRequest httpReq)
		{
			var resolvedPathInfo = httpReq.PathInfo;

			var pos = httpReq.AbsoluteUri.IndexOf(resolvedPathInfo, StringComparison.InvariantCultureIgnoreCase);
			if (pos == -1)
				throw new ArgumentException(
					string.Format("PathInfo '{0}' is not in Url '{1}'", resolvedPathInfo, httpReq.RawUrl));

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
			if (aspNetReq != null)
			{
				return aspNetReq.Request.PhysicalPath;
			}

			var filePath = httpReq.ApplicationFilePath.CombineWith(httpReq.PathInfo);
			return filePath;
		}

		public static string GetApplicationUrl(this HttpRequest httpReq)
		{
			var appPath = httpReq.ApplicationPath;
			var baseUrl = httpReq.Url.Scheme + "://" + httpReq.Url.Host;
			if (httpReq.Url.Port != 80) baseUrl += ":" + httpReq.Url.Port;
			var appUrl = baseUrl.CombineWith(appPath);
			return appUrl;
		}
	}
}