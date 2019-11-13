using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IHttpRestClientAsync : IRestClientAsync
    {
        Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl);
        Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl);
        Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request);
        Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request);
        Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request);
        Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default);
    }
}