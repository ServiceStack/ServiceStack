using System;
using System.IO;

namespace ServiceStack.Service
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient, IReplyClient
	{
	}

}