using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IRestClientAsync : IServiceClientCommon
    {
        Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default);
        Task<TResponse> GetAsync<TResponse>(object requestDto, CancellationToken token = default);
        Task GetAsync(IReturnVoid requestDto, CancellationToken token = default);

        Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default);
        Task<TResponse> DeleteAsync<TResponse>(object requestDto, CancellationToken token = default);
        Task DeleteAsync(IReturnVoid requestDto, CancellationToken token = default);

        Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default);
        Task<TResponse> PostAsync<TResponse>(object requestDto, CancellationToken token = default);
        Task PostAsync(IReturnVoid requestDto, CancellationToken token = default);

        Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default);
        Task<TResponse> PutAsync<TResponse>(object requestDto, CancellationToken token = default);
        Task PutAsync(IReturnVoid requestDto, CancellationToken token = default);

        Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default);
        Task<TResponse> PatchAsync<TResponse>(object requestDto, CancellationToken token = default);
        Task PatchAsync(IReturnVoid requestDto, CancellationToken token = default);

        Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, CancellationToken token = default);
        Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto, CancellationToken token = default);
        Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto, CancellationToken token = default);
    }
}