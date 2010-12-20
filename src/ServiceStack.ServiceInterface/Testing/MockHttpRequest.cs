using System;
using System.Collections.Specialized;
using System.IO;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Testing
{
	public class MockHttpRequest : IHttpRequest
	{
		public MockHttpRequest()
		{
		}

		public MockHttpRequest(string operationName, string httpMethod,
			string contentType, string pathInfo,
			NameValueCollection queryString, Stream inputStream, NameValueCollection formData)
		{
			this.OperationName = operationName;
			this.HttpMethod = httpMethod;
			this.ContentType = contentType;
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

		private string rawBody;
		public string GetRawBody()
		{
			if (rawBody != null) return rawBody;
			if (InputStream == null) return null;
			using (var reader = new StreamReader(InputStream))
			{
				rawBody = reader.ReadToEnd();
				return rawBody;
			}
		}

		public string RawUrl { get; set; }

		public string AbsoluteUri
		{
			get { return "http://localhost" + this.PathInfo; }
		}

		public string UserHostAddress { get; set; }
		public bool IsSecureConnection { get; set; }
		public string[] AcceptTypes { get; set; }
		public string PathInfo { get; set; }
		public Stream InputStream { get; set; }

		public long ContentLength
		{
			get
			{
				var body = GetRawBody();
				return body != null ? body.Length : 0;
			}
		}
	}
}