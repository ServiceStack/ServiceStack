using System;
using System.IO;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpResponseWrapper
		: IHttpResponse
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseWrapper));

		private readonly HttpResponse response;

		public HttpResponseWrapper(HttpResponse response)
		{
			this.response = response;
		}

		public int StatusCode
		{
			set { this.response.StatusCode = value; }
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
			try
			{
				response.Flush();
				response.Close();
			}
			catch (Exception ex)
			{
				Log.Error("Exception closing HttpResponse: " + ex.Message, ex);
			}
		}

		public bool IsClosed
		{
			get;
			private set;
		}
	}
}