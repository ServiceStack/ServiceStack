using System;

namespace ServiceStack
{
	public interface IRestClientAsync : IDisposable
	{
		void SetCredentials(string userName, string password);

        void GetAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void GetAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void GetAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);

        void DeleteAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void DeleteAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);

        void PostAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void PostAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);

        void PutAsync<TResponse>(IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void PutAsync<TResponse>(object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);

        void CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
        void CustomMethodAsync<TResponse>(string httpVerb, object requestDto, Action<TResponse> onSuccess, Action<TResponse, Exception> onError);
	    void CancelAsync();
	}

}