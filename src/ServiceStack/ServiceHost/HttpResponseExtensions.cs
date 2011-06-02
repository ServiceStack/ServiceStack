using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceHost
{
	public static class HttpResponseExtensions
	{
		public static void RedirectToUrl(this IHttpResponse httpRes, string url)
		{
			httpRes.AddHeader(HttpHeaders.Location, url);
			httpRes.Close();
		}

		public static void TransmitFile(this IHttpResponse httpRes, string filePath)
		{
			var aspNetRes = httpRes as HttpResponseWrapper;
			if (aspNetRes != null)
			{
				aspNetRes.Response.TransmitFile(filePath);
				return;
			}

			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				fs.WriteTo(httpRes.OutputStream);
			}

			httpRes.Close();
		}

		public static void WriteFile(this IHttpResponse httpRes, string filePath)
		{
			var aspNetRes = httpRes as HttpResponseWrapper;
			if (aspNetRes != null)
			{
				aspNetRes.Response.WriteFile(filePath);
				return;
			}

			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				fs.WriteTo(httpRes.OutputStream);
			}

			httpRes.Close();
		}

		public static void Redirect(this IHttpResponse httpRes, string url)
		{
			httpRes.AddHeader(HttpHeaders.Location, url);
			httpRes.Close();
		}

		public static void ReturnAuthRequired(this IHttpResponse httpRes)
		{
			httpRes.ReturnAuthRequired("Auth Required");
		}

		public static void ReturnAuthRequired(this IHttpResponse httpRes, string authRealm)
		{
			httpRes.StatusCode = (int) HttpStatusCode.Unauthorized;
			httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "Basic realm=\"" + authRealm + "\"");
			httpRes.Close();
		}

		/// <summary>
		/// Sets a persistent cookie which never expires
		/// </summary>
		public static void SetPermanentCookie(this IHttpResponse httpRes, string cookieName, string cookieValue)
		{
			SetCookie(httpRes, cookieName, cookieValue, DateTime.UtcNow.AddYears(20), null);
		}

		/// <summary>
		/// Sets a session cookie which expires after the browser session closes
		/// </summary>
		public static void SetSessionCookie(this IHttpResponse httpRes, string cookieName, string cookieValue)
		{
			var cookie = String.Format("{0}={1};path=/", cookieName, cookieValue);
			httpRes.AddHeader(HttpHeaders.SetCookie, cookie);
		}

		/// <summary>
		/// Sets a persistent cookie which expires after the given time
		/// </summary>
		public static void SetCookie(this IHttpResponse httpRes, string cookieName, string cookieValue, TimeSpan expiresIn)
		{
			var expiration = DateTime.UtcNow + expiresIn;
			SetCookie(httpRes, cookieName, cookieValue, expiration, null);
		}

		/// <summary>
		/// Sets a persistent cookie with an expiresAt date
		/// </summary>
		public static void SetCookie(this IHttpResponse httpRes, string cookieName, 
			string cookieValue, DateTime expiresAt, string path)
		{
			path = path ?? "/";
			var cookie = String.Format("{0}={1};expires={2};path={3}", cookieName, cookieValue, expiresAt.ToString("R"), path);
			httpRes.AddHeader(HttpHeaders.SetCookie, cookie);
		}

		/// <summary>
		/// Deletes a specified cookie by setting its value to empty and expiration to -1 days
		/// </summary>
		public static void DeleteCookie(this IHttpResponse httpRes, string cookieName)
		{
			var cookie = String.Format("{0}=;expires={1};path=/",
				cookieName, DateTime.UtcNow.AddDays(-1).ToString("R"));
			httpRes.AddHeader(HttpHeaders.SetCookie, cookie);
		}

		public static void AddHeaderLastModified(this IHttpResponse httpRes, DateTime lastModified)
		{
			var lastWt = lastModified.ToUniversalTime();
			httpRes.AddHeader(HttpHeaders.LastModified, lastWt.ToString("r"));
		}
	}
}