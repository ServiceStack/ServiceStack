using System;
using System.Net;
using ServiceStack.Common;

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

	}
}