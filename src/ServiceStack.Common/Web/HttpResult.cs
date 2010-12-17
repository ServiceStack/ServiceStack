using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ServiceStack.Service;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common.Web
{
	public class HttpResult
		: IHttpResult, IStreamWriter
	{
		public HttpResult()
			: this (null, null)
		{
		}

		public HttpResult(object response)
			: this(response, null)
		{
		}

		public HttpResult(object response, string contentType)
			: this(response, contentType, HttpStatusCode.OK)
		{
		}

		public HttpResult(object response, string contentType, HttpStatusCode statusCode)
		{
			this.Headers = new Dictionary<string, string>();
			this.ResponseFilter = HttpResponseFilter.Instance;

			this.Response = response;
			this.ContentType = contentType;
			this.StatusCode = statusCode;
		}

		public IContentTypeWriter ResponseFilter { get; set; }

		public string ContentType { get; set; }

		public Dictionary<string, string> Headers { get; private set; }
		
		public IDictionary<string, string> Options
		{
			get { return this.Headers; }
		}

		public HttpStatusCode StatusCode { get; set; }

		public object Response { get; set; }

		public void WriteTo(Stream responseStream)
		{
			if (this.ResponseFilter == null)
				throw new ArgumentNullException("ResponseFilter");

			ResponseFilter.SerializeToStream(this.ContentType, this.Response, responseStream);
		}
	}

}