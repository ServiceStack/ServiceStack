using System;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IRestClientAsync : IServiceClientCommon
    {
        Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto);
        Task<TResponse> GetAsync<TResponse>(object requestDto);
        Task GetAsync(IReturnVoid requestDto);

        Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto);
        Task<TResponse> DeleteAsync<TResponse>(object requestDto);
        Task DeleteAsync(IReturnVoid requestDto);

        Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto);
        Task<TResponse> PostAsync<TResponse>(object requestDto);
        Task PostAsync(IReturnVoid requestDto);

        Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto);
        Task<TResponse> PutAsync<TResponse>(object requestDto);
        Task PutAsync(IReturnVoid requestDto);

        Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto);
        Task<TResponse> PatchAsync<TResponse>(object requestDto);
        Task PatchAsync(IReturnVoid requestDto);

        Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
        Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto);
        Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto);
    }
}