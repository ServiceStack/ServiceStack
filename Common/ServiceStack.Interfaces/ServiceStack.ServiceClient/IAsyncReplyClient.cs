using System;

namespace ServiceStack.ServiceClient
{
	public interface IAsyncReplyClient
	{
		IAsyncResult BeginSend(object request);
		TResponse EndSend<TResponse>(IAsyncResult asyncResult, TimeSpan timeout);
	}
}