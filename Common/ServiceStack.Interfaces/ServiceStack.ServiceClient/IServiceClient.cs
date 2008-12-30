using System;

namespace ServiceStack.ServiceClient
{
	public interface IServiceClient : IOneWayClient, IReplyClient, IDisposable
	{
		
	}
}