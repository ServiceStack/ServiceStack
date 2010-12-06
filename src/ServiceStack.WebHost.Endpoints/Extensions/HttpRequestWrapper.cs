using System.Collections.Specialized;
using System.IO;
using System.Web;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	internal class HttpRequestWrapper
		: IHttpRequest
	{
		private readonly HttpRequest request;

		public HttpRequestWrapper(string operationName, HttpRequest response)
		{
			this.OperationName = operationName;
			this.request = response;
		}

		public string OperationName { get; set; }

		public string HttpMethod
		{
			get { return request.HttpMethod; }
		}

		public NameValueCollection QueryString
		{
			get { return request.QueryString; }
		}

		public NameValueCollection FormData
		{
			get { return request.Form; }
		}

		public string RawBody
		{
			get
			{
				using (var reader = new StreamReader(request.InputStream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}