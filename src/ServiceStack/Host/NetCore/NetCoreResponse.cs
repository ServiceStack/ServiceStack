#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Web;
using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ServiceStack.Host.NetCore
{
    public class NetCoreResponse : IHttpResponse
    {
        private readonly NetCoreRequest request;
        private readonly HttpResponse response;

        public NetCoreResponse(NetCoreRequest request, HttpResponse response)
        {
            this.request = request;
            this.response = response;
            this.Items = new Dictionary<string, object>();
            this.Cookies = new NetCoreCookies(response);
            response.StatusCode = 200;
        }

        public void AddHeader(string name, string value)
        {
            response.Headers.Add(name, new StringValues(value));
        }

        public string GetHeader(string name)
        {
            var values = response.Headers[name];
            return values.ToString();
        }

        public void Redirect(string url)
        {
            response.Redirect(url);
        }

        public void Write(string text)
        {
            var bytes = text.ToUtf8Bytes();
            response.Body.Write(bytes, 0, bytes.Length);
        }

        bool closed = false;

        public void Close()
        {
            if (closed) return;
            Flush();
            response.Body.Close();
            closed = true;
        }

        public void End()
        {
            if (closed) return;
            Flush();
            response.Body.Dispose();
            closed = true;
        }

        public void Flush()
        {
            if (closed) return;
            response.Body.Flush();
        }

        public void SetContentLength(long contentLength)
        {
            response.ContentLength = contentLength;
        }

        public object OriginalResponse => response;

        public IRequest Request => request;

        public int StatusCode
        {
            get { return response.StatusCode; }
            set { response.StatusCode = value; }
        }

        public string StatusDescription { get; set; } // Not available in .NET Core

        public string ContentType
        {
            get { return response.ContentType; }
            set { response.ContentType = value; }
        }

        public Stream OutputStream => response.Body;

        public object Dto { get; set; }

        public bool UseBufferedStream { get; set; }

        public bool IsClosed { get; set; }

        public bool KeepAlive { get; set; }

        public Dictionary<string, object> Items { get; set; }

        public void SetCookie(Cookie cookie)
        {
            response.Cookies.Append(cookie.Name, cookie.Value, new CookieOptions {
                Domain = cookie.Domain,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly,
                Path = cookie.Path,
                Secure = cookie.Secure,
            });
        }

        public void ClearCookies()
        {
            //response.Cookies.Delete();
        }

        public ICookies Cookies { get; }
    }

    public class NetCoreCookies : ICookies
    {
        public const string RootPath = "/";
        private HttpResponse response;

        public NetCoreCookies(HttpResponse response)
        {
            this.response = response;
        }

        public void DeleteCookie(string cookieName)
        {
            response.Cookies.Delete(cookieName);
        }

        public void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
            var options = new CookieOptions
            {
                Path = RootPath,
                Expires = DateTime.UtcNow.AddYears(20)
            };
            if (secureOnly != null)
            {
                options.Secure = secureOnly.Value;
            }
            response.Cookies.Append(cookieName, cookieValue, options);
        }

        public void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
            var options = new CookieOptions
            {
                Path = RootPath,
            };
            if (secureOnly != null)
            {
                options.Secure = secureOnly.Value;
            }
            response.Cookies.Append(cookieName, cookieValue, options);
        }
    }
}

#endif