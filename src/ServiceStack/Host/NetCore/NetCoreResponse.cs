#if NETCORE

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
using ServiceStack.Text;

namespace ServiceStack.Host.NetCore
{
    public class NetCoreResponse : IHttpResponse, IHasHeaders
    {
        private static ILog Log = LogManager.GetLogger(typeof(NetCoreResponse));

        private readonly NetCoreRequest request;
        private readonly HttpResponse response;
        private bool hasResponseBody;

        public HttpContext HttpContext => response.HttpContext;
        public HttpResponse HttpResponse => response;


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

        public MemoryStream BufferedStream { get; set; }
        public Stream OutputStream => BufferedStream ?? response.Body;

        public bool UseBufferedStream
        {
            get => BufferedStream != null;
            set => BufferedStream = value
                ? BufferedStream ?? this.CreateBufferedStream()
                : null;
        }

        bool closed = false;

        public void Close()
        {
            if (closed) return;
            closed = true;
            try
            {
                this.FlushBufferIfAny(BufferedStream, response.Body);
                BufferedStream?.Dispose();
                BufferedStream = null;
            }
            catch (Exception ex)
            {
                Log.Error("Error closing .NET Core OutputStream", ex);
            }
        }

        public async Task CloseAsync(CancellationToken token = default)
        {
            if (!closed)
            {
                closed = true;
                try
                {
                    await this.FlushBufferIfAnyAsync(BufferedStream, response.Body, token: token);
                    BufferedStream?.Dispose();
                    BufferedStream = null;
                }
                catch (Exception ex)
                {
                    Log.Error("Error closing .NET Core OutputStream", ex);
                }
            }
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

            this.FlushBufferIfAny(BufferedStream, response.Body);
            this.AllowSyncIO();
            response.Body.Flush();
        }

        public async Task FlushAsync(CancellationToken token = default(CancellationToken))
        {
            await this.FlushBufferIfAnyAsync(BufferedStream, response.Body, token);
            await response.Body.FlushAsync(token);
        }

        public void SetContentLength(long contentLength)
        {
            if (!response.HasStarted && contentLength >= 0)
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

        public object Dto { get; set; }

        public bool IsClosed => closed;

        public bool KeepAlive { get; set; }

        public bool HasStarted => response.HasStarted;

        public Dictionary<string, object> Items { get; set; }

        public void SetCookie(Cookie cookie)
        {
            try
            {
                if (!HostContext.AppHost.SetCookieFilter(Request, cookie))
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

        public Dictionary<string, string> Headers
        {
            get
            {
                var to = new Dictionary<string, string>();
                foreach (var entry in response.Headers)
                {
                    to[entry.Key] = entry.Value.ToString();
                }
                return to;
            }
        }
    }
}

#endif