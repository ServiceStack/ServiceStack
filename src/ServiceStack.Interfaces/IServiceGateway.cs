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
        /// <summary>
        /// Normal Request/Reply Services
        /// </summary>
        TResponse Send<TResponse>(object requestDto);

        /// <summary>
        /// Auto Batched Request/Reply Requests
        /// </summary>
        List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos);

        /// <summary>
        /// OneWay Service
        /// </summary>
        void Publish(object requestDto);

        /// <summary>
        /// Auto Batched OneWay Requests
        /// </summary>
        void PublishAll(IEnumerable<object> requestDtos);
    }

    /// <summary>
    /// The minimal API Surface to capture the most common ASYNC requests.
    /// Convenience extensions over these core API's available in ServiceGatewayExtensions
    /// </summary>
    public interface IServiceGatewayAsync
    {
        /// <summary>
        /// Normal Request/Reply Services
        /// </summary>
        Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token);

        /// <summary>
        /// Auto Batched Request/Reply Requests
        /// </summary>
        Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token);

        /// <summary>
        /// OneWay Service
        /// </summary>
        Task PublishAsync(object requestDto, CancellationToken token);

        /// <summary>
        /// Auto Batched OneWay Requests
        /// </summary>
        Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token);
    }
}

