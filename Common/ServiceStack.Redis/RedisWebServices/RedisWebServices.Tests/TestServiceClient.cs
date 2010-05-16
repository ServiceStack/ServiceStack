using System;
using ServiceStack.Service;
using ServiceStack.WebHost.Endpoints;

namespace RedisWebServices.Tests
{
	public class TestServiceClient
		: IServiceClient
	{
		private readonly AppHostBase appHostBase;

		public TestServiceClient(AppHostBase appHostBase)
		{
			this.appHostBase = appHostBase;
		}

		public TResponse Send<TResponse>(object request)
		{
			return (TResponse) appHostBase.ExecuteService(request);
		}

		public void SendOneWay(object request)
		{
			appHostBase.ExecuteService(request);
		}

		public void Dispose()
		{
		}
	}
}