#if !NETSTANDARD2_0

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.AspNet
{
    public class AspNetResponse : IHttpResponse
    {
        //private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseWrapper));

        private readonly HttpResponseBase response;

        public AspNetResponse(HttpResponseBase response, IRequest request = null)
        {
            this.response = response;
            this.Request = request;
            this.response.TrySkipIisCustomErrors = true;
            this.Cookies = HostContext.AppHost.GetCookies(this);
            this.Items = new Dictionary<string, object>();
        }

        public HttpResponseBase Response => response;

        public object OriginalResponse => response;

        public IRequest Request { get; private set; }

        public int StatusCode
        {
            get => this.response.StatusCode;
            set => this.response.StatusCode = value;
        }

        public string StatusDescription
        {
            get => this.response.StatusDescription;
            set => this.response.StatusDescription = value;
        }

        public string ContentType
        {
            get => response.ContentType;
            set => response.ContentType = value;
        }

        public void AddHeader(string name, string value)
        {
            response.AddHeader(name, value);
        }

        public void RemoveHeader(string name)
        {
            response.Headers.Remove(name);
        }

        public string GetHeader(string name)
        {
            return response.Headers[name];
        }

        public void Redirect(string url)
        {
            response.Redirect(url);
        }

        public MemoryStream BufferedStream { get; set; }
        public Stream OutputStream => BufferedStream ?? response.OutputStream;

        public bool UseBufferedStream
        {
            get { return BufferedStream != null; }
            set
            {
                if (true)
                    this.response.BufferOutput = false;

                BufferedStream = value
                    ? BufferedStream ?? new MemoryStream()
                    : null;
            }
        }

        private void FlushBufferIfAny()
        {
            if (BufferedStream == null)
                return;

            var bytes = BufferedStream.ToArray();
            try {
                SetContentLength(bytes.Length); //safe to set Length in Buffered Response
            } catch {}

            response.OutputStream.Write(bytes, 0, bytes.Length);
            BufferedStream = MemoryStreamFactory.GetStream();
        }

        public object Dto { get; set; }

        public void Close()
        {
            this.IsClosed = true;

            FlushBufferIfAny();

            response.CloseOutputStream();
        }

        public void End()
        {
            this.IsClosed = true;
            try
            {
                FlushBufferIfAny();

                response.ClearContent();
                response.End();
            }
            catch { }
        }

        public void Flush()
        {
            FlushBufferIfAny();

            response.Flush();
        }

        public Task FlushAsync(CancellationToken token = default(CancellationToken))
        {
            Flush(); //FlushAsync() only added in .NET 4.6

            return TypeConstants.EmptyTask;
        }

        public bool IsClosed
        {
            get;
            private set;
        }

        public void SetContentLength(long contentLength)
        {
            try
            {
                if (contentLength >= 0)
                    response.Headers["Content-Length"] = contentLength.ToString(CultureInfo.InvariantCulture);
            }
            catch (PlatformNotSupportedException /*ignore*/) { } //This operation requires IIS integrated pipeline mode.
        }

        //Benign, see how to enable in ASP.NET: http://technet.microsoft.com/en-us/library/cc772183(v=ws.10).aspx
        public bool KeepAlive { get; set; }

        /// <summary>
        /// Can ignore as doesn't throw if HTTP Headers already written
        /// </summary>
        public bool HasStarted => false;

        public Dictionary<string, object> Items { get; }

        public ICookies Cookies { get; set; }

        public void SetCookie(Cookie cookie)
        {
            if (!HostContext.AppHost.AllowSetCookie(Request, cookie.Name))
                return;

            var httpCookie = cookie.ToHttpCookie();
            response.SetCookie(httpCookie);            
        }

        public void ClearCookies()
        {
            response.Cookies.Clear();
        }
    }
}

#endif
