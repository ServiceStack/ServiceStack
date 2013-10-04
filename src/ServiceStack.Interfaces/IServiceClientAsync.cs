using System;

namespace ServiceStack
{
	public interface IServiceClientAsync : IRestClientAsync
	{
		void SendAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
	}
}