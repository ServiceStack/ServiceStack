using System;

namespace ServiceStack.Service
{
	public interface IAsyncReplyClient
	{
		void Send<TResponse>(object request, Action<TResponse> callback);
	}
}