using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    /// <summary>
    /// The minimal API Surface to capture the most common SYNC requests.
    /// Convenience extensions over these core API's available in ServiceGatewayExtensions
    /// </summary>
    public interface IServiceGateway
    {
        TResponse Send<TResponse>(object requestDto);

        List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos);

        void Publish(object requestDto);
    }

    /// <summary>
    /// The minimal API Surface to capture the most common ASYNC requests.
    /// Convenience extensions over these core API's available in ServiceGatewayExtensions
    /// </summary>
    public interface IServiceGatewayAsync
    {
        Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token);

        Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token);

        Task PublishAsync(object requestDto, CancellationToken token);
    }
}

