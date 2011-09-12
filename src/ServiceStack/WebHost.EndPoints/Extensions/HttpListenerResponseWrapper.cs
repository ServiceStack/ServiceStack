using System;
using System.IO;
using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using System.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpListenerResponseWrapper 
		: IHttpResponse
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (HttpListenerResponseWrapper));

		private readonly HttpListenerResponse response;
        private MemoryStream _buf = new MemoryStream(2000);
        private TextWriter _tw;

		public HttpListenerResponseWrapper(HttpListenerResponse response)
		{
			this.response = response;
            _tw = new StreamWriter(_buf, ContentEncoding);
		}

		public int StatusCode
		{
			set { this.response.StatusCode = value; }
		}

        public string StatusDescription
        {
            set { this.response.StatusDescription = value; }
        }

		public string ContentType
		{
			get { return response.ContentType; }
			set { response.ContentType = value; }
		}

		public void AddHeader(string name, string value)
		{
			response.AddHeader(name, value);
		}

		public void Redirect(string url)
		{
			response.Redirect(url);
		}

		public Stream OutputStream
		{
            get { return _buf; }
		}

        public Encoding ContentEncoding
        {
            get { return response.ContentEncoding == null ? Encoding.UTF8 : response.ContentEncoding; }
            set 
            { 
                response.ContentEncoding = value;
                _tw.Flush();
                _tw = new StreamWriter(_buf, value);
            }
        }

		public void Write(string text)
		{
            _tw.Write(text);
            _tw.Flush();
		}

        public void Clear()
        {
            _buf = new MemoryStream();
            _tw = new StreamWriter(_buf, ContentEncoding);
        }

		public void Close()
		{
            if (!this.IsClosed)
            {
                _tw.Flush();
                if (_buf.Length > 0)
                {
                    if (response.ContentEncoding == null) response.ContentEncoding = ContentEncoding;
                    response.ContentLength64 = _buf.Length;
                    var d = _buf.GetBuffer();
                    this.response.OutputStream.Write(d, 0, (int) _buf.Length);
                }
                this.IsClosed = true;
            }
		}

		public bool IsClosed
		{
			get;
			private set;
		}


        public TextWriter Output
        {
            get { return _tw; }
        }
    }

}