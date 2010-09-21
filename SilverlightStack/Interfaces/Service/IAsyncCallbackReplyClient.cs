using System;

namespace ServiceStack.Service
{
	public interface IAsyncCallbackReplyClient
	{
		IAsyncResult BeginSend(object request, AsyncCallback callback, object state);
		TResponse EndSend<TResponse>(IAsyncResult asyncResult, TimeSpan timeout);
	}
}