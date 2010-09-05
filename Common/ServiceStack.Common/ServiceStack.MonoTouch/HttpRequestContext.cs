using System;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost
{
	public class HttpRequestContext 
		: IRequestContext
	{
		public HttpRequestContext(object dto)
			: this(dto, null)
		{
		}

		public HttpRequestContext(object dto, EndpointAttributes endpointAttributes)
			: this(dto, endpointAttributes, null)
		{
		}

		public HttpRequestContext(object requestDto, IFactoryProvider factory)
			: this(requestDto, EndpointAttributes.None, factory)
		{
		}

		public HttpRequestContext(object dto, EndpointAttributes endpointAttributes, IFactoryProvider factory)
		{
			this.Dto = dto;
			this.EndpointAttributes = endpointAttributes;
			this.Factory = factory;
			this.RequestAttributes = new RequestAttributes(HttpContext.Current);
		}

		public bool AutoDispose { get; set; }

		public object Dto { get; set; }

		public EndpointAttributes EndpointAttributes { get; private set; }

		public IRequestAttributes RequestAttributes { get; private set; }

		public T Get<T>() where T : class
		{
			var isDto = this.Dto as T;
			return isDto ?? (this.Factory != null ? this.Factory.Resolve<T>() : null);
		}

		public IFactoryProvider Factory { get; set; }

		public string MimeType
		{
			get
			{
				if ((this.EndpointAttributes & EndpointAttributes.Json) == EndpointAttributes.Json)
					return MimeTypes.Json;

				if ((this.EndpointAttributes & EndpointAttributes.Xml) == EndpointAttributes.Xml)
					return MimeTypes.Xml;

				if ((this.EndpointAttributes & EndpointAttributes.Jsv) == EndpointAttributes.Jsv)
					return MimeTypes.Jsv;

				if ((this.EndpointAttributes & EndpointAttributes.Csv) == EndpointAttributes.Csv)
					return MimeTypes.Csv;

				if ((this.EndpointAttributes & EndpointAttributes.ProtoBuf) == EndpointAttributes.ProtoBuf)
					return MimeTypes.ProtoBuf;

				return null;
			}
		}

		public string CompressionType
		{
			get
			{
				if (this.RequestAttributes.AcceptsDeflate)
					return CompressionTypes.Deflate;

				if (this.RequestAttributes.AcceptsGzip)
					return CompressionTypes.GZip;

				return null;
			}
		}

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

			if (this.Factory != null)
			{
				this.Factory.Dispose();
			}
		}
	}
}