using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public static class HttpRequestAuthentication
	{
		public static string GetBasicAuth(this IHttpRequest httpReq)
		{
			var auth = httpReq.Headers[HttpHeaders.Authorization];
			if (auth == null) return null;

			var parts = auth.Split(' ');
			if (parts.Length != 2) return null;
			return parts[0].ToLower() == "basic" ? parts[1] : null;
		}

		public static KeyValuePair<string, string>? GetBasicAuthUserAndPassword(this IHttpRequest httpReq)
		{
			var userPassBase64 = httpReq.GetBasicAuth();
			if (userPassBase64 == null) return null;
			var userPass = Encoding.UTF8.GetString(Convert.FromBase64String(userPassBase64));
			var parts = userPass.SplitOnFirst(':');
			return new KeyValuePair<string, string>(parts[0], parts[1]);
		}

		public static string GetCookieValue(this IHttpRequest httpReq, string cookieName)
		{
			Cookie cookie;
			httpReq.Cookies.TryGetValue(cookieName, out cookie);
			return cookie != null ? cookie.Value : null;
		}

	}
}