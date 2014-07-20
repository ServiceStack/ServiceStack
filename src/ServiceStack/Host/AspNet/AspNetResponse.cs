//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host.AspNet
{
    public class AspNetResponse : IHttpResponse
    {
        //private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseWrapper));

        private readonly HttpResponseBase response;

        public AspNetResponse(HttpResponseBase response)
        {
            this.response = response;
            this.response.TrySkipIisCustomErrors = true;
            this.Cookies = new Cookies(this);
        }

        public HttpResponseBase Response
        {
            get { return response; }
        }

        public object OriginalResponse
        {
            get { return response; }
        }

        public int StatusCode
        {
            get { return this.response.StatusCode; }
            set { this.response.StatusCode = value; }
        }

        public string StatusDescription
        {
            get { return this.response.StatusDescription; }
            set { this.response.StatusDescription = value; }
        }

        public string ContentType
        {
            get { return response.ContentType; }
            set { response.ContentType = value; }
        }

        public ICookies Cookies { get; set; }

        public void AddHeader(string name, string value)
        {
            response.AddHeader(name, value);
        }

        public void Redirect(string url)
        {
            response.Redirect(url);
        }

        private MemoryStream bufferedStream;
        public Stream OutputStream
        {
            get { return bufferedStream ?? response.OutputStream; }
        }

        public bool UseBufferedStream
        {
            get { return bufferedStream != null; }
            set
            {
                bufferedStream = value
                    ? bufferedStream ?? new MemoryStream()
                    : null;
            }
        }

        private void FlushBufferIfAny()
        {
            if (bufferedStream == null)
                return;

            var bytes = bufferedStream.ToArray();
            try {
                SetContentLength(bytes.LongLength); //safe to set Length in Buffered Response
            } catch {}

            response.OutputStream.Write(bytes, 0, bytes.Length);
            bufferedStream = new MemoryStream();
        }

        public object Dto { get; set; }

        public void Write(string text)
        {
            response.Write(text);
        }

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

        public bool IsClosed
        {
            get;
            private set;
        }

        public void SetContentLength(long contentLength)
        {
            try
            {
                response.Headers.Add("Content-Length", contentLength.ToString(CultureInfo.InvariantCulture));
            }
            catch (PlatformNotSupportedException /*ignore*/) { } //This operation requires IIS integrated pipeline mode.
        }

        //Benign, see how to enable in ASP.NET: http://technet.microsoft.com/en-us/library/cc772183(v=ws.10).aspx
        public bool KeepAlive { get; set; }

        public void SetCookie(Cookie cookie)
        {
            var httpCookie = cookie.ToHttpCookie();
            response.SetCookie(httpCookie);            
        }
    }
}