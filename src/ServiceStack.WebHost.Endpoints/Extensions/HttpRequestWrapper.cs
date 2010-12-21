using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using ServiceStack.ServiceHost;

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
		
		public string ContentType
		{
			get { return request.ContentType; }
		}

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

		public string GetRawBody()
		{
			using (var reader = new StreamReader(request.InputStream))
			{
				return reader.ReadToEnd();
			}
		}

		public string RawUrl
		{
			get { return request.RawUrl; }
		}

		public string AbsoluteUri
		{
			get
			{
				try
				{
					return request.Url.AbsoluteUri;
				}
				catch (Exception ex)
				{
					//fastcgi mono, do a 2nd rounds best efforts
					return "http://" + request.UserHostName + request.RawUrl;
				}
			}
		}

		public string UserHostAddress
		{
			get { return request.UserHostAddress; }
		}

		public bool IsSecureConnection
		{
			get { return request.IsSecureConnection; }
		}

		public string[] AcceptTypes
		{
			get { return request.AcceptTypes; }
		}

		public string PathInfo
		{
			get { return request.GetPathInfo(); }
		}

		public Stream InputStream
		{
			get { return request.InputStream; }
		}

		public long ContentLength
		{
			get { return request.ContentLength; }
		}
	}
}