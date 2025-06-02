#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public interface IRestGatewayAsync
{
    Task<T> SendAsync<T>(IReturn<T> request, CancellationToken token);
    Task<T> GetAsync<T>(IReturn<T> request, CancellationToken token);
    Task<T> PostAsync<T>(IReturn<T> request, CancellationToken token);
    Task<T> PutAsync<T>(IReturn<T> request, CancellationToken token);
    Task<T> DeleteAsync<T>(IReturn<T> request, CancellationToken token);
}