using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
	public class HttpRequestContext
		: IRequestContext
	{
		private readonly IHttpRequest httpReq;
		private readonly IHttpResponse httpRes;

		public HttpRequestContext(object dto)
            : this(null, null, dto, RequestAttributes.None)
		{
		}

		public HttpRequestContext(object dto, RequestAttributes requestAttributes)
            : this(null, null, dto, requestAttributes)
		{
		}

		public HttpRequestContext(IHttpRequest httpReq, IHttpResponse httpRes, object dto)
			: this(httpReq, httpRes, dto, RequestAttributes.None)
		{
		}

		public HttpRequestContext(IHttpRequest httpReq, IHttpResponse httpRes, object dto, RequestAttributes requestAttributes)
		{
			this.httpReq = httpReq;
			this.httpRes = httpRes;
            this.Dto = dto;
            this.RequestAttributes = requestAttributes;
            
            this.Files = this.httpReq != null ? httpReq.Files : new IHttpFile[0];
		    this.RequestPreferences = HttpContext.Current == null && httpReq != null
		        ? new RequestPreferences(httpReq)
		        : new RequestPreferences(HttpContext.Current);
        }

		public bool AutoDispose { get; set; }

		public object Dto { get; set; }

		public IDictionary<string, System.Net.Cookie> Cookies
		{
			get { return this.httpReq.Cookies; }
		}

		public RequestAttributes RequestAttributes { get; private set; }

		public Web.IRequestPreferences RequestPreferences { get; private set; }
		
		public string ContentType
		{
			get { return this.httpReq.ContentType; }
		}

	    private string responseContentType;
		public string ResponseContentType
		{
            get { return responseContentType ?? this.httpReq.ResponseContentType; }
            set { responseContentType = value; }
		}

		public T Get<T>() where T : class
		{
			if (typeof(T) == typeof(IHttpRequest))
				return (T)this.httpReq;
			if (typeof(T) == typeof(IHttpResponse))
				return (T)this.httpRes;

			var isDto = this.Dto as T;
		    return isDto ?? HostContext.TryResolve<T>();
		}

		public string GetHeader(string headerName)
		{
			return this.httpReq.Headers.Get(headerName);
		}

		public string MimeType
		{
			get
			{
				if ((this.RequestAttributes & RequestAttributes.Json) == RequestAttributes.Json)
					return MimeTypes.Json;

				if ((this.RequestAttributes & RequestAttributes.Xml) == RequestAttributes.Xml)
					return MimeTypes.Xml;

				if ((this.RequestAttributes & RequestAttributes.Jsv) == RequestAttributes.Jsv)
					return MimeTypes.Jsv;

				if ((this.RequestAttributes & RequestAttributes.Csv) == RequestAttributes.Csv)
					return MimeTypes.Csv;

				if ((this.RequestAttributes & RequestAttributes.ProtoBuf) == RequestAttributes.ProtoBuf)
					return MimeTypes.ProtoBuf;

				return null;
			}
		}

		public string CompressionType
		{
			get
			{
				if (this.RequestPreferences.AcceptsDeflate)
					return CompressionTypes.Deflate;

				if (this.RequestPreferences.AcceptsGzip)
					return CompressionTypes.GZip;

				return null;
			}
		}

		public string AbsoluteUri
		{
			get
			{
				return this.httpReq != null ? this.httpReq.AbsoluteUri : null;
			}
		}

		public string PathInfo
		{
			get { return this.httpReq != null ? this.httpReq.PathInfo : null; }
		}

		public IHttpFile[] Files { get; set; }

		private string ipAddress;
		public string IpAddress
		{
			get
			{
				if (ipAddress == null)
				{
					ipAddress = GetIpAddress();
				}
				return ipAddress;
			}
		}

		public static string GetIpAddress()
		{
			return HttpContext.Current != null
				? HttpContext.Current.Request.UserHostAddress
				: null;
		}

		~HttpRequestContext()
		{
			if (this.AutoDispose)
			{
				Dispose(false);
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public virtual void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);
		}
	}
}