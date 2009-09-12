using System;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.LogicFacade;
using ServiceStack.Service;

namespace ServiceStack.ServiceInterface
{
	public class RequestContext : IRequestContext
	{
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

		public object Dto { get; set; }

		public EndpointAttributes EndpointAttributes { get; private set; }

		public IRequestAttributes RequestAttributes { get; private set; }

		public ICacheTextManager CacheTextManager { get; set; }

		public T Get<T>() where T : class
		{
			var isDto = this.Dto as T;
			return isDto ?? this.Factory.Resolve<T>();
		}

		public IFactoryProvider Factory { get; set; }


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

			var context = System.ServiceModel.OperationContext.Current;
			return context == null ? null : GetIpAddress(context);
		}

		public static string GetIpAddress(System.ServiceModel.OperationContext context)
		{
			var prop = context.IncomingMessageProperties;
			if (context.IncomingMessageProperties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
			{
				var endpoint = prop[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]
					as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
				if (endpoint != null)
				{
					return endpoint.Address;
				}
			}
			return null;
		}

		~RequestContext()
		{
			Dispose(false);
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