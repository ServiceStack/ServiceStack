using System.Collections.Specialized;
using System.IO;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.IntegrationTests.Testing
{
	public class MockHttpRequest : IHttpRequest
	{
		public MockHttpRequest()
		{
		}

		public MockHttpRequest(string operationName, string httpMethod, 
			string pathInfo,  NameValueCollection queryString, Stream inputStream, 
			NameValueCollection formData)
		{
			this.OperationName = operationName;
			this.HttpMethod = httpMethod;
			this.PathInfo = pathInfo;
			this.QueryString = queryString;
			this.FormData = formData;
			this.InputStream = inputStream;
		}

		public string OperationName { get; set; }
		public string ContentType { get; set; }
		public string HttpMethod { get; set; }
		
		public NameValueCollection QueryString { get; set; }

		public NameValueCollection FormData { get; set; }
		
		public string GetRawBody()
		{
			if (InputStream == null) return null;
			using (var reader = new StreamReader(InputStream))
			{
				return reader.ReadToEnd();
			}
		}

		public string RawUrl { get; set; }
		public string UserHostAddress { get; set; }
		public bool IsSecureConnection { get; set; }
		public string[] AcceptTypes { get; set; }
		public string PathInfo { get; set; }
		public Stream InputStream { get; set; }
	}
}