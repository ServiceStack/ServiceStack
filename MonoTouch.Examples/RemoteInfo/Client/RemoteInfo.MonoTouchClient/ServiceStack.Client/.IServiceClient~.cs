
using System;

namespace ServiceStack.Client
{
	public interface IServiceClient : IDisposable
	{
		TResponse Send<TResponse>(object request);		
	
		void SendOneWay(object request);
	}
}
