using System;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.ServiceInterface;

namespace ServiceStack.ServiceHost
{
	public class RequestContext 
		: IRequestContext
	{
		public RequestContext(object dto)
			: this(dto, null)
		{
		}

		public RequestContext(object dto, EndpointAttributes endpointAttributes)
			: this(dto, endpointAttributes, null)
		{
		}

		public RequestContext(object requestDto, IFactoryProvider factory)
			: this(requestDto, EndpointAttributes.None, factory)
		{
		}

		public RequestContext(object dto, EndpointAttributes endpointAttributes, IFactoryProvider factory)
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

		public string ContentType
		{
			get
			{
				if ((this.EndpointAttributes & EndpointAttributes.Json) == EndpointAttributes.Json)
					return MimeTypes.Json;

				if ((this.EndpointAttributes & EndpointAttributes.Xml) == EndpointAttributes.Xml)
					return MimeTypes.Xml;

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

		public string IpAddress
		{
			get
			{
				return GetIpAddress();
			}
		}

		public static string GetIpAddress()
		{
			if (HttpContext.Current != null)
			{
				return HttpContext.Current.Request.UserHostAddress;
			}
			return null;
		}

		~RequestContext()
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