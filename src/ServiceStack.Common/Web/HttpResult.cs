using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Web
{
	public class HttpResult
		: IHttpResult, IStreamWriter
	{
		public HttpResult()
		{
			this.Headers = new Dictionary<string, string>();
			this.StatusCode = HttpStatusCode.OK;
		}

		public string contentType;
		public string ContentType 
		{ 
			get
			{
				if (contentType != null) return contentType;
				this.Headers.TryGetValue(HttpHeaders.ContentType, out contentType);
				return contentType;
			}
			set
			{
				contentType = value;
				this.Headers[HttpHeaders.ContentType] = contentType;
			}
		}

		public Dictionary<string, string> Headers { get; private set; }
		
		public IDictionary<string, string> Options
		{
			get { return this.Headers; }
		}

		public HttpStatusCode StatusCode { get; set; }

		public object Response { get; set; }

		public void WriteTo(Stream stream)
		{
			switch (contentType)
			{
				case Web.ContentType.Xml:
					XmlSerializer.SerializeToStream(Response, stream);
					break;
				case Web.ContentType.Json:
					JsonSerializer.SerializeToStream(Response, stream);
					break;
				case Web.ContentType.Jsv:
					TypeSerializer.SerializeToStream(Response, stream);
					break;
				default:
					throw new NotSupportedException("ContentType not supported: " + contentType);
			}
		}
	}

}