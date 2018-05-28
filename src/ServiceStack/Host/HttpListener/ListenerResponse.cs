#if !NETSTANDARD2_0

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.HttpListener
{
    public class ListenerResponse : IHttpResponse
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ListenerResponse));

        private readonly HttpListenerResponse response;

        public ListenerResponse(HttpListenerResponse response, IRequest request = null)
        {
            this.response = response;
            this.Request = request;
            this.Cookies = HostContext.AppHost.GetCookies(this);
            this.Items = new Dictionary<string, object>();
        }

        public object OriginalResponse => response;

        public IRequest Request { get; }

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
            get => BufferedStream != null;
            set => BufferedStream = value
                ? BufferedStream ?? new MemoryStream()
                : null;
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
            if (!this.IsClosed)
            {
                this.IsClosed = true;

                try
                {
                    FlushBufferIfAny();

                    this.response.CloseOutputStream();
                }
                catch (Exception ex)
                {
                    Log.Error("Error closing HttpListener output stream", ex);
                }
            }
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
            FlushBufferIfAny();

            response.OutputStream.Flush();
        }

        public async Task FlushAsync(CancellationToken token = new CancellationToken())
        {
            if (BufferedStream != null)
            {
                var bytes = BufferedStream.ToArray();
                try
                {
                    SetContentLength(bytes.Length); //safe to set Length in Buffered Response
                }
                catch { }

                await response.OutputStream.WriteAsync(bytes, token);
                BufferedStream = MemoryStreamFactory.GetStream();
            }

            await response.OutputStream.FlushAsync(token);
        }

        public bool IsClosed
        {
            get;
            private set;
        }

        public void SetContentLength(long contentLength)
        {
            //you can happily set the Content-Length header in Asp.Net
            //but HttpListener will complain if you do - you have to set ContentLength64 on the response.
            //workaround: HttpListener throws "The parameter is incorrect" exceptions when we try to set the Content-Length header
            if (contentLength >= 0)
                response.ContentLength64 = contentLength;
        }

        public bool KeepAlive
        {
            get => response.KeepAlive;
            set => response.KeepAlive = true;
        }

        /// <summary>
        /// Can ignore as doesn't throw if HTTP Headers already written
        /// </summary>
        public bool HasStarted => false;

        public Dictionary<string, object> Items { get; private set; }

        public ICookies Cookies { get; set; }

        public void SetCookie(Cookie cookie)
        {
            if (!HostContext.AppHost.AllowSetCookie(Request, cookie.Name))
                return;

            var cookieStr = cookie.AsHeaderValue();
            response.Headers.Add(HttpHeaders.SetCookie, cookieStr);            
        }

        public void ClearCookies()
        {
            response.Headers.Remove(HttpHeaders.SetCookie);
        }
    }

}

#endif