using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.Web;
using HttpResponseWrapper = ServiceStack.WebHost.Endpoints.Wrappers.HttpResponseWrapper;

namespace ServiceStack
{
	public static class HttpResponseExtensions
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensions));
        //public static bool IsXsp;
        //public static bool IsModMono;
        public static bool IsMonoFastCgi;
        //public static bool IsWebDevServer;
        //public static bool IsIis;
        public static bool IsHttpListener;

        static HttpResponseExtensions()
        {
            //IsXsp = Env.IsMono;
            //IsModMono = Env.IsMono;
            IsMonoFastCgi = Env.IsMono;

            //IsWebDevServer = !Env.IsMono;
            //IsIis = !Env.IsMono;
            IsHttpListener = HttpContext.Current == null;
        }

        public static void CloseOutputStream(this HttpResponse response)
        {
            try
            {
                //Don't close for MonoFastCGI as it outputs random 4-letters at the start
                if (!IsMonoFastCgi)
                {
                    response.OutputStream.Flush();
                    response.OutputStream.Close();
                    //response.Close(); //This kills .NET Development Web Server
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception closing HttpResponse: " + ex.Message, ex);
            }
        }

        public static void CloseOutputStream(this HttpListenerResponse response)
        {
            try
            {
                response.OutputStream.Flush();
                response.OutputStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Error in HttpListenerResponseWrapper: " + ex.Message, ex);
            }
        }

		public static void RedirectToUrl(this IHttpResponse httpRes, string url, HttpStatusCode redirectStatusCode=HttpStatusCode.Redirect)
		{
		    httpRes.StatusCode = (int) redirectStatusCode;
			httpRes.AddHeader(HttpHeaders.Location, url);
            httpRes.EndRequest();
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

            httpRes.EndRequest();
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

            httpRes.EndRequest();
        }

		public static void Redirect(this IHttpResponse httpRes, string url)
		{
			httpRes.AddHeader(HttpHeaders.Location, url);
            httpRes.EndRequest();
        }

		public static void ReturnAuthRequired(this IHttpResponse httpRes)
		{
			httpRes.ReturnAuthRequired("Auth Required");
		}

		public static void ReturnAuthRequired(this IHttpResponse httpRes, string authRealm)
		{
            httpRes.ReturnAuthRequired(AuthenticationHeaderType.Basic, authRealm);
		}

        public static void ReturnAuthRequired(this IHttpResponse httpRes, AuthenticationHeaderType AuthType, string authRealm)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, string.Format("{0} realm=\"{1}\"",AuthType.ToString(),authRealm));
            httpRes.EndRequest();
        }

		/// <summary>
		/// Sets a persistent cookie which never expires
		/// </summary>
		public static void SetPermanentCookie(this IHttpResponse httpRes, string cookieName, string cookieValue)
		{
			httpRes.Cookies.AddPermanentCookie(cookieName, cookieValue);
		}

		/// <summary>
		/// Sets a session cookie which expires after the browser session closes
		/// </summary>
		public static void SetSessionCookie(this IHttpResponse httpRes, string cookieName, string cookieValue)
		{
			httpRes.Cookies.AddSessionCookie(cookieName, cookieValue);
		}

		/// <summary>
		/// Sets a persistent cookie which expires after the given time
		/// </summary>
		public static void SetCookie(this IHttpResponse httpRes, string cookieName, string cookieValue, TimeSpan expiresIn)
		{
			httpRes.Cookies.AddCookie(new Cookie(cookieName, cookieValue) {
				Expires = DateTime.UtcNow + expiresIn
			});
		}

		/// <summary>
		/// Sets a persistent cookie with an expiresAt date
		/// </summary>
		public static void SetCookie(this IHttpResponse httpRes, string cookieName,
			string cookieValue, DateTime expiresAt, string path = "/")
		{
			httpRes.Cookies.AddCookie(new Cookie(cookieName, cookieValue, path) {
				Expires = expiresAt,
			});
		}

		/// <summary>
		/// Deletes a specified cookie by setting its value to empty and expiration to -1 days
		/// </summary>
		public static void DeleteCookie(this IHttpResponse httpRes, string cookieName)
		{
			httpRes.Cookies.DeleteCookie(cookieName);
		}

		public static Dictionary<string, string> CookiesAsDictionary(this IHttpResponse httpRes)
		{
			var map = new Dictionary<string, string>();
			var aspNet = httpRes.OriginalResponse as System.Web.HttpResponse;
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
				var httpListener = httpRes.OriginalResponse as HttpListenerResponse;
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

		public static void AddHeaderLastModified(this IHttpResponse httpRes, DateTime? lastModified)
		{
			if (!lastModified.HasValue) return;
			var lastWt = lastModified.Value.ToUniversalTime();
			httpRes.AddHeader(HttpHeaders.LastModified, lastWt.ToString("r"));
		}
	}

    public enum AuthenticationHeaderType
    {
        Basic,
        Digest
    }
}