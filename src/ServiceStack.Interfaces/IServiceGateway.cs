using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IServiceGateway : IReplyClient {}
    public interface IReplyClient
    {
        TResponse Send<TResponse>(object request);

        TResponse Send<TResponse>(IReturn<TResponse> request);

        List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests);

        void Send(IReturnVoid request);

        void Publish(object requestDto);
    }

    public interface IServiceGatewayAsync
    {
        Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token);

        Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token);

        Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requests, CancellationToken token);

        Task SendAsync(IReturnVoid requestDto, CancellationToken token);

        Task PublishAsync(object requestDto, CancellationToken token);
    }
}

