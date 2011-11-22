using System;
using System.IO;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpResponseWrapper
		: IHttpResponse
	{
		//private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseWrapper));

		private readonly HttpResponse response;

		public HttpResponseWrapper(HttpResponse response)
		{
			this.response = response;
		}

		public HttpResponse Response
		{
			get { return response; }
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
			get { return response.OutputStream; }
		}

		public void Write(string text)
		{
			response.Write(text);
		}

		public void Close()
		{
			this.IsClosed = true;
			response.CloseOutputStream();
		}

		public void Flush()
		{
			response.Flush();
		}

		public bool IsClosed
		{
			get;
			private set;
		}
	}
}