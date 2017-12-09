#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Web;
using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using ServiceStack.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Host.NetCore
{
    public class NetCoreResponse : IHttpResponse
    {
        private static ILog Log = LogManager.GetLogger(typeof(NetCoreResponse));

        private readonly NetCoreRequest request;
        private readonly HttpResponse response;
        private bool hasResponseBody;

        public NetCoreResponse(NetCoreRequest request, HttpResponse response)
        {
            this.request = request;
            this.response = response;
            this.Items = new Dictionary<string, object>();
            this.Cookies = HostContext.AppHost.GetCookies(this);

            //Don't set StatusCode here as it disables Redirects from working in MVC 
            //response.StatusCode = 200;
        }

        public void AddHeader(string name, string value)
        {
            try
            {
                if (response.Headers.TryGetValue(name, out var values))
                {
                    string[] existingValues = values.ToArray();
                    if (!existingValues.Contains(value))
                    {
                        var newValues = new string[existingValues.Length + 1];
                        existingValues.CopyTo(newValues, 0);
                        newValues[newValues.Length - 1] = value;
                        response.Headers[name] = new StringValues(newValues);
                    }
                }
                else
                {
                    response.Headers.Add(name, new StringValues(value));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed Adding Headers[{name}]={value}: {ex.Message}", ex);
            }
        }

        public void RemoveHeader(string name)
        {
            response.Headers.Remove(name);
        }

        public string GetHeader(string name)
        {
            var values = response.Headers[name];
            return values.Count > 0 ? values.ToString() : null;
        }

        public void Redirect(string url)
        {
            response.Redirect(url);
        }

        bool closed = false;

        public void Close()
        {
            closed = true;
        }

        public void End()
        {
            if (closed) return;
            if (!hasResponseBody && !response.HasStarted)
                response.ContentLength = 0;

            Flush();
            closed = true;
        }

        public void Flush()
        {
            if (closed) return;
            response.Body.Flush();
        }

        public Task FlushAsync(CancellationToken token = new CancellationToken())
        {
            if (closed) return TypeConstants.EmptyTask;
            return response.Body.FlushAsync(token);
        }

        public void SetContentLength(long contentLength)
        {
            if (request.HttpMethod == HttpMethods.Head || Platforms.PlatformNetCore.HostInstance.Config?.DisableChunkedEncoding == true && contentLength >= 0)
                response.ContentLength = contentLength;
            
            if (contentLength > 0)
                hasResponseBody = true;
        }

        public object OriginalResponse => response;

        public IRequest Request => request;

        public int StatusCode
        {
            get => response.StatusCode;
            set => response.StatusCode = value;
        }

        public string StatusDescription 
        { 
            get => response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase;
            set => response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = value;
        }

        public string ContentType
        {
            get => response.ContentType;
            set => response.ContentType = value;
        }

        public Stream OutputStream => response.Body;

        public object Dto { get; set; }

        public bool UseBufferedStream { get; set; }

        public bool IsClosed => closed; 

        public bool KeepAlive { get; set; }

        public bool HasStarted => response.HasStarted;

        public Dictionary<string, object> Items { get; set; }

        public void SetCookie(Cookie cookie)
        {
            try
            {
                if (!HostContext.AppHost.AllowSetCookie(Request, cookie.Name))
                    return;

                var cookieOptions = cookie.ToCookieOptions();
                response.Cookies.Append(cookie.Name, cookie.Value, cookieOptions);
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not Set-Cookie '{cookie.Name}': " + ex.Message, ex);
            }
        }

        public void ClearCookies()
        {
            response.Headers.Remove(HttpHeaders.SetCookie);
        }

        public ICookies Cookies { get; }
    }
}

#endif