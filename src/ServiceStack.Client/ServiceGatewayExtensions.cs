using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class ServiceGatewayExtensions
    {
        public static TResponse Send<TResponse>(this IServiceGateway client, IReturn<TResponse> request)
        {
            return client.Send<TResponse>(request);
        }

        public static void Send(this IServiceGateway client, IReturnVoid request)
        {
            client.Send<byte[]>(request);
        }

        public static List<TResponse> SendAll<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> request)
        {
            return client.SendAll<TResponse>(request);
        }

    }

    public static class ServiceGatewayAsyncWrappers
    {
        public static Task<TResponse> SendAsync<TResponse>(this IServiceGateway client, IReturn<TResponse> requestDto, CancellationToken token = default(CancellationToken))
        {
            return client.SendAsync<TResponse>((object)requestDto, token);
        }

        public static Task<TResponse> SendAsync<TResponse>(this IServiceGateway client, object requestDto, CancellationToken token = default(CancellationToken))
        {
            var nativeAsync = client as IServiceGatewayAsync;
            return nativeAsync != null
                ? nativeAsync.SendAsync<TResponse>(requestDto, token)
                : Task.Factory.StartNew(() => client.Send<TResponse>(requestDto), token);
        }

        public static Task SendAsync(this IServiceGateway client, IReturnVoid requestDto, CancellationToken token = default(CancellationToken))
        {
            var nativeAsync = client as IServiceGatewayAsync;
            return nativeAsync != null
                ? nativeAsync.SendAsync<byte[]>(requestDto, token)
                : Task.Factory.StartNew(() => client.Send<byte[]>(requestDto), token);
        }

        public static Task<List<TResponse>> SendAllAsync<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token = default(CancellationToken))
        {
            var nativeAsync = client as IServiceGatewayAsync;
            return nativeAsync != null
                ? nativeAsync.SendAllAsync<TResponse>(requestDtos, token)
                : Task.Factory.StartNew(() => client.SendAll<TResponse>(requestDtos), token);
        }

        public static Task PublishAsync(this IServiceGateway client, object requestDto, CancellationToken token = default(CancellationToken))
        {
            var nativeAsync = client as IServiceGatewayAsync;
            return nativeAsync != null
                ? nativeAsync.PublishAsync(requestDto, token)
                : Task.Factory.StartNew(() => client.Publish(requestDto), token);
        }

        public static Task PublishAllAsync(this IServiceGateway client, IEnumerable<object> requestDtos, CancellationToken token = default(CancellationToken))
        {
            var nativeAsync = client as IServiceGatewayAsync;
            return nativeAsync != null
                ? nativeAsync.PublishAllAsync(requestDtos, token)
                : Task.Factory.StartNew(() => client.PublishAll(requestDtos), token);
        }
    }
}