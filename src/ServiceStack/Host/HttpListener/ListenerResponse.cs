//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Host.HttpListener
{
    public class ListenerResponse : IHttpResponse
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ListenerResponse));

        private readonly System.Net.HttpListenerResponse response;

        public ListenerResponse(System.Net.HttpListenerResponse response)
        {
            this.response = response;
            this.Cookies = new Cookies(this);
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
            response.OutputStream.Write(bytes, 0, bytes.Length);
            bufferedStream = new MemoryStream();
        }

        public object Dto { get; set; }

        public void Write(string text)
        {
            try
            {
                var bOutput = System.Text.Encoding.UTF8.GetBytes(text);
                response.ContentLength64 = bOutput.Length;

                OutputStream.Write(bOutput, 0, bOutput.Length);
                Close();
            }
            catch (Exception ex)
            {
                Log.Error("Could not WriteTextToResponse: " + ex.Message, ex);
                throw;
            }
        }

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
            response.ContentLength64 = contentLength;
        }

        public void SetCookie(Cookie cookie)
        {
            var cookieStr = cookie.AsHeaderValue();
            response.Headers.Add(HttpHeaders.SetCookie, cookieStr);            
        }
    }

}