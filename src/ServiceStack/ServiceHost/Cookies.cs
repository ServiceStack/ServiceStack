using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Common.Web;

namespace ServiceStack.ServiceHost
{
	public class Cookies : ICookies
	{
		readonly IHttpResponse httpRes;
		private static readonly DateTime Session = DateTime.MinValue;
		private static readonly DateTime Permanaent = DateTime.UtcNow.AddYears(20);
		private const string RootPath = "/";

		public Cookies(IHttpResponse httpRes)
		{
			this.httpRes = httpRes;
		}

		/// <summary>
		/// Sets a persistent cookie which never expires
		/// </summary>
		public void AddPermanentCookie(string cookieName, string cookieValue)
		{
			AddCookie(new Cookie(cookieName, cookieValue, RootPath) {
				Expires = Permanaent,
			});
		}

		/// <summary>
		/// Sets a session cookie which expires after the browser session closes
		/// </summary>
		public void AddSessionCookie(string cookieName, string cookieValue)
		{
			AddCookie(new Cookie(cookieName, cookieValue, RootPath));
		}

		/// <summary>
		/// Deletes a specified cookie by setting its value to empty and expiration to -1 days
		/// </summary>
		public void DeleteCookie(string cookieName)
		{
			var cookie = String.Format("{0}=;expires={1};path=/",
				cookieName, DateTime.UtcNow.AddDays(-1).ToString("R"));
			httpRes.AddHeader(HttpHeaders.SetCookie, cookie);
		}

		public HttpCookie ToHttpCookie(Cookie cookie)
		{
			return new HttpCookie(cookie.Name, cookie.Value) {
				Path = cookie.Path,
				Expires = cookie.Expires,
				Domain = (string.IsNullOrEmpty(cookie.Domain)) ? null : cookie.Domain,
			};
		}

		public string GetHeaderValue(Cookie cookie)
		{
			return cookie.Expires == Session
				? String.Format("{0}={1};path=/", cookie.Name, cookie.Value)
				: String.Format("{0}={1};expires={2};path={3}",
					cookie.Name, cookie.Value, cookie.Expires.ToString("R"), cookie.Path ?? "/");
		}

		/// <summary>
		/// Sets a persistent cookie which expires after the given time
		/// </summary>
		public void AddCookie(Cookie cookie)
		{
			var aspNet = this.httpRes.OriginalResponse as HttpResponse;
			if (aspNet != null)
			{
				var httpCookie = ToHttpCookie(cookie);
				aspNet.SetCookie(httpCookie);
				return;
			}
			var httpListener = this.httpRes.OriginalResponse as HttpListenerResponse;
			if (httpListener != null)
			{
				var cookieStr = GetHeaderValue(cookie);
				httpListener.Headers.Add(HttpHeaders.SetCookie, cookieStr);
			}
		}
	}
}