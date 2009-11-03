using System;
using ServiceStack.Service;

namespace ServiceStack.Messaging.Client
{
	public class AmqpServiceClient
		: IServiceClient
	{
		public void SendOneWay(object request)
		{
			throw new NotImplementedException();
		}

		public TResponse Send<TResponse>(object request)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}