using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpResponseWrapper : IHttpResponse
	{
		private readonly HttpResponse response;

		public HttpResponseWrapper(HttpResponse response)
		{
			this.response = response;
		}

		public string ContentType
		{
			get { return response.ContentType; }
			set { response.ContentType = value; }
		}

		public NameValueCollection Headers
		{
			get 
			{
				return response.Headers; 
			}
		}

		public Stream OutputStream
		{
			get { return response.OutputStream; }
		}

		public void Write(string text)
		{
			response.Write(text);
		}
	}
}