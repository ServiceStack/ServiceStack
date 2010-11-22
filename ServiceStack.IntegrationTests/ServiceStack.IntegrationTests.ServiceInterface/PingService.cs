using System;
using ServiceStack.IntegrationTests.ServiceModel;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class PingService
		: IService<Ping>
	{
		public object Execute(Ping request)
		{
			return new PingResponse { Text = "Pong " + request.Text };
		}
	}
}