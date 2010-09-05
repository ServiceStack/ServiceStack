using System;

namespace ServiceStack.Service
{
	public interface IAsyncReplyClient
	{
		IAsyncResult BeginSend(object request, AsyncCallback callback, object state);
		TResponse EndSend<TResponse>(IAsyncResult asyncResult, TimeSpan timeout);
	}
}