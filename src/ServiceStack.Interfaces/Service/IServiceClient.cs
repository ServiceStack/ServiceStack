using System;

namespace ServiceStack.Service
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient
#if !(SILVERLIGHT || MONOTOUCH || ANDROIDINDIE)
		, IReplyClient
#endif
	{
	}

}