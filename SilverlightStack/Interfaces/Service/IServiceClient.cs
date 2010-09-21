using System;

namespace ServiceStack.Service
{
	public interface IServiceClient : IOneWayClient, IReplyClient, IDisposable
	{
		
	}
}