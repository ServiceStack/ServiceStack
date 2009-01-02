using System;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
{
	public class RequestContext : IRequestContext
	{
		public RequestContext(object requestDto, IFactoryProvider factory)
		{
			this.Dto = requestDto;
			this.Factory = factory;
		}

		public object Dto { get; set; }

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
				if (HttpContext.Current != null)
				{
					return HttpContext.Current.Request.UserHostAddress;
				}

				var context = System.ServiceModel.OperationContext.Current;
				if (context == null) return null;
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