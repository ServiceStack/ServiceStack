using System.IO;
using System.Web;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpResponseWrapper 
		: IHttpResponse
	{
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
	}
}