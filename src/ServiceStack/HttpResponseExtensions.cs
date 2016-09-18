using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

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
        public static bool IsNetCore;

        static HttpResponseExtensions()
        {
            //IsXsp = Env.IsMono;
            //IsModMono = Env.IsMono;
            IsMonoFastCgi = Env.IsMono;

            //IsWebDevServer = !Env.IsMono;
            //IsIis = !Env.IsMono;
#if !NETSTANDARD1_6
            IsHttpListener = HttpContext.Current == null;
#else
            IsNetCore = true;
#endif
        }

#if !NETSTANDARD1_6
        public static void CloseOutputStream(this HttpResponseBase response)
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
#endif

        public static void RedirectToUrl(this IResponse httpRes, string url, HttpStatusCode redirectStatusCode = HttpStatusCode.Redirect)
        {
            httpRes.StatusCode = (int)redirectStatusCode;
            httpRes.AddHeader(HttpHeaders.Location, url);
            httpRes.EndRequest();
        }

        public static void TransmitFile(this IResponse httpRes, string filePath)
        {
#if !NETSTANDARD1_6
            var aspNetRes = httpRes as ServiceStack.Host.AspNet.AspNetResponse;
            if (aspNetRes != null)
            {
                aspNetRes.Response.TransmitFile(filePath);
                return;
            }
#endif

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.WriteTo(httpRes.OutputStream);
            }

            httpRes.EndRequest();
        }

        public static void WriteFile(this IResponse httpRes, string filePath)
        {
#if !NETSTANDARD1_6
            var aspNetRes = httpRes as ServiceStack.Host.AspNet.AspNetResponse;
            if (aspNetRes != null)
            {
                aspNetRes.Response.WriteFile(filePath);
                return;
            }
#endif

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.WriteTo(httpRes.OutputStream);
            }

            httpRes.EndRequest();
        }

        public static void Redirect(this IResponse httpRes, string url)
        {
            httpRes.AddHeader(HttpHeaders.Location, url);
            httpRes.EndRequest();
        }

        public static void ReturnFailedAuthentication(this IAuthSession session, IRequest request)
        {
            var authFeature = HostContext.GetPlugin<AuthFeature>();
            if (authFeature != null)
            {
                var defaultAuth = AuthenticateService.AuthProviders.FirstOrDefault() as AuthProvider;
                if (defaultAuth != null)
                {
                    defaultAuth.OnFailedAuthentication(session, request, request.Response);
                    return;
                }
            }
            request.Response.ReturnAuthRequired();
        }

        public static void ReturnAuthRequired(this IResponse httpRes)
        {
            httpRes.ReturnAuthRequired("Auth Required");
        }

        public static void ReturnAuthRequired(this IResponse httpRes, string authRealm)
        {
            httpRes.ReturnAuthRequired(AuthenticationHeaderType.Basic, authRealm);
        }

        public static void ReturnAuthRequired(this IResponse httpRes, AuthenticationHeaderType AuthType, string authRealm)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, $"{AuthType} realm=\"{authRealm}\"");
            httpRes.EndRequest();
        }

        public static void ClearCookies(this IResponse response)
        {
            var httpRes = response as IHttpResponse;
            httpRes?.ClearCookies();
        }

        /// <summary>
        /// Sets a persistent cookie which never expires
        /// </summary>
        public static void SetPermanentCookie(this IResponse response, string cookieName, string cookieValue)
        {
            var httpRes = response as IHttpResponse;
            httpRes?.Cookies.AddPermanentCookie(cookieName, cookieValue);
        }

        /// <summary>
        /// Sets a session cookie which expires after the browser session closes
        /// </summary>
        public static void SetSessionCookie(this IResponse response, string cookieName, string cookieValue)
        {
            var httpRes = response as IHttpResponse;
            httpRes?.Cookies.AddSessionCookie(cookieName, cookieValue);
        }

        /// <summary>
        /// Sets a persistent cookie which expires after the given time
        /// </summary>
        public static void SetCookie(this IResponse response, string cookieName, string cookieValue, TimeSpan expiresIn)
        {
            response.SetCookie(new Cookie(cookieName, cookieValue)
            {
                Expires = DateTime.UtcNow + expiresIn
            });
        }

        public static void SetCookie(this IResponse response, Cookie cookie)
        {
            var httpRes = response as IHttpResponse;
            httpRes?.SetCookie(cookie);
        }

        /// <summary>
        /// Sets a persistent cookie with an expiresAt date
        /// </summary>
        public static void SetCookie(this IResponse response, string cookieName,
            string cookieValue, DateTime expiresAt, string path = "/")
        {
            response.SetCookie(new Cookie(cookieName, cookieValue, path)
            {
                Expires = expiresAt,
            });
        }

        /// <summary>
        /// Deletes a specified cookie by setting its value to empty and expiration to -1 days
        /// </summary>
        public static void DeleteCookie(this IResponse response, string cookieName)
        {
            var httpRes = response as IHttpResponse;
            httpRes?.Cookies.DeleteCookie(cookieName);
        }

        public static Dictionary<string, string> CookiesAsDictionary(this IResponse httpRes)
        {
            return Platform.Instance.GetCookiesAsDictionary(httpRes);
        }

        public static void AddHeaderLastModified(this IResponse httpRes, DateTime? lastModified)
        {
            if (!lastModified.HasValue) return;
            var lastWt = lastModified.Value.ToUniversalTime();
            httpRes.AddHeader(HttpHeaders.LastModified, lastWt.ToString("r"));
        }

        public static string SetParam(this string url, string key, object val)
        {
            return url.SetParam(key, val.ToString());
        }

        public static string SetParam(this string url, string key, string val)
        {
            var addToQueryString = HostContext.Config.AddRedirectParamsToQueryString;
            return addToQueryString
                ? url.SetQueryParam(key, val)
                : url.SetHashParam(key, val);
        }

        public static string AddParam(this string url, string key, object val)
        {
            return url.AddParam(key, val.ToString());
        }

        public static string AddParam(this string url, string key, string val)
        {
            var addToQueryString = HostContext.Config.AddRedirectParamsToQueryString;
            return addToQueryString
                ? url.AddQueryParam(key, val)
                : url.AddHashParam(key, val);
        }
    }

    public enum AuthenticationHeaderType
    {
        Basic,
        Digest
    }
}