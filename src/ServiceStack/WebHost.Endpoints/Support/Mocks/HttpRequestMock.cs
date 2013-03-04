using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support.Mocks
{
	public class HttpRequestMock : IHttpRequest
	{
		public HttpRequestMock()
		{
			this.Headers = new NameValueCollection();
			this.Cookies = new Dictionary<string, Cookie>();
			this.FormData = new NameValueCollection();
			this.QueryString = new NameValueCollection();
			this.Items = new Dictionary<string, object>();
		}

		public HttpRequestMock(string operationName, string httpMethod, 
		                       string contentType, string pathInfo,  
		                       NameValueCollection queryString, Stream inputStream, NameValueCollection formData)
			: this()
		{
			this.OperationName = operationName;
			this.HttpMethod = httpMethod;
			this.ContentType = contentType;
			this.PathInfo = pathInfo;
			this.QueryString = queryString;
			this.FormData = formData;
			this.InputStream = inputStream;
		}

		public object OriginalRequest
		{
			get { return null; }
		}

		public T TryResolve<T>()
		{
			return EndpointHost.AppHost.TryResolve<T>();
		}

		public string OperationName { get; set; }
		public string ContentType { get; set; }
		public string HttpMethod { get; set; }

	    public bool IsLocal
	    {
	        get { return true; }
	    }

	    public string UserAgent { get; set; }

		public IDictionary<string, Cookie> Cookies { get; set; }

		private string responseContentType;
		public string ResponseContentType
		{
			get { return responseContentType ?? this.ContentType ?? Common.Web.ContentType.Json; }
			set { responseContentType = value; }
		}

		public NameValueCollection Headers { get; set; }

		public NameValueCollection QueryString { get; set; }

		public NameValueCollection FormData { get; set; }

        public bool UseBufferedStream { get; set; }

	    public Dictionary<string, object> Items
		{
			get; private set;
		}

		private string rawBody;
		public string GetRawBody()
		{
			if (rawBody != null) return rawBody;
			if (InputStream == null) return null;

			//Keep the stream alive in-case it needs to be read twice (i.e. ContentLength)
			rawBody = new StreamReader(InputStream).ReadToEnd();
			InputStream.Position = 0;
			return rawBody;
		}

		public string RawUrl { get; set; }

		public string AbsoluteUri
		{
			get { return "http://localhost" + this.PathInfo; }
		}

		public string UserHostAddress { get; set; }

        public string RemoteIp { get; set; }
        public string XForwardedFor { get; set; }
        public string XRealIp { get; set; }

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

		public IFile[] Files { get; set; }

		public string ApplicationFilePath
		{
			get { throw new NotImplementedException(); }
		}
	}
}