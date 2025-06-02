#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public interface IHttpRestClientAsync : IRestClientAsync
{
    Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token = default);
    Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token = default);
    Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token = default);
    Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token = default);
    Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request, CancellationToken token = default);
    Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default);
}