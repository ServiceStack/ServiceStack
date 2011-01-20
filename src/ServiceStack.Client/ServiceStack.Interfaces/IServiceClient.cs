
using System;

namespace ServiceStack.Service
{
	public interface IServiceClient : IDisposable
	{
		TResponse Send<TResponse>(object request);		
	
		void SendOneWay(object request);
	}
}
