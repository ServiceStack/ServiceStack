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

        public static object Send(this IServiceGateway client, Type resposneType, object request)
        {
            if (!LateBoundSendSyncFns.TryGetValue(resposneType, out var sendFn))
            {
                var mi = typeof(ServiceGatewayExtensions).GetStaticMethod("SendObject");
                var genericMi = mi.MakeGenericMethod(resposneType);
                sendFn = (Func<IServiceGateway, object, object>)
                    genericMi.CreateDelegate(typeof(Func<IServiceGateway, object, object>));

                Dictionary<Type, Func<IServiceGateway, object, object>> snapshot, newCache;
                do
                {
                    snapshot = LateBoundSendSyncFns;
                    newCache = new Dictionary<Type, Func<IServiceGateway, object, object>>(LateBoundSendSyncFns)
                    {
                        [resposneType] = sendFn
                    };

                } while (!ReferenceEquals(
                    Interlocked.CompareExchange(ref LateBoundSendSyncFns, newCache, snapshot), snapshot));
            }
            return sendFn(client, request);
        }

        public static Task<object> SendAsync(this IServiceGateway client, Type resposneType, object request, CancellationToken token = default(CancellationToken))
        {
            if (!LateBoundSendAsyncFns.TryGetValue(resposneType, out var sendFn))
            {
                var mi = typeof(ServiceGatewayExtensions).GetStaticMethod("SendObjectAsync");
                var genericMi = mi.MakeGenericMethod(resposneType);
                sendFn = (Func<IServiceGateway, object, CancellationToken, Task<object>>)
                    genericMi.CreateDelegate(typeof(Func<IServiceGateway, object, CancellationToken, Task<object>>));

                Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>> snapshot, newCache;
                do
                {
                    snapshot = LateBoundSendAsyncFns;
                    newCache = new Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>>(LateBoundSendAsyncFns) {
                        [resposneType] = sendFn
                    };

                } while (!ReferenceEquals(
                    Interlocked.CompareExchange(ref LateBoundSendAsyncFns, newCache, snapshot), snapshot));
            }
            return sendFn(client, request, token);
        }

        public static Type GetResponseType(this IServiceGateway client, object request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var returnTypeDef = request.GetType().GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
            if (returnTypeDef == null)
                throw new ArgumentException("Late-bound Send<object> can only be called for Request DTO's implementing IReturn<T>");

            var resposneType = returnTypeDef.GetGenericArguments()[0];
            return resposneType;
        }

        private static Dictionary<Type, Func<IServiceGateway, object, object>> LateBoundSendSyncFns =
            new Dictionary<Type, Func<IServiceGateway, object, object>>();

        internal static object SendObject<TResponse>(IServiceGateway client, object request)
        {
            return client.Send<TResponse>(request);
        }

        private static Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>> LateBoundSendAsyncFns =
            new Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>>();

        internal static Task<object> SendObjectAsync<TResponse>(IServiceGateway client, object request, CancellationToken token)
        {
            return client.SendAsync<TResponse>(request, token).ContinueWith(x => (object)x.Result, token);
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
            return client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.SendAsync<TResponse>(requestDto, token)
                : Task.Factory.StartNew(() => client.Send<TResponse>(requestDto), token);
        }

        public static Task SendAsync(this IServiceGateway client, IReturnVoid requestDto, CancellationToken token = default(CancellationToken))
        {
            return client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.SendAsync<byte[]>(requestDto, token)
                : Task.Factory.StartNew(() => client.Send<byte[]>(requestDto), token);
        }

        public static Task<List<TResponse>> SendAllAsync<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token = default(CancellationToken))
        {
            return client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.SendAllAsync<TResponse>(requestDtos, token)
                : Task.Factory.StartNew(() => client.SendAll<TResponse>(requestDtos), token);
        }

        public static Task PublishAsync(this IServiceGateway client, object requestDto, CancellationToken token = default(CancellationToken))
        {
            return client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.PublishAsync(requestDto, token)
                : Task.Factory.StartNew(() => client.Publish(requestDto), token);
        }

        public static Task PublishAllAsync(this IServiceGateway client, IEnumerable<object> requestDtos, CancellationToken token = default(CancellationToken))
        {
            return client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.PublishAllAsync(requestDtos, token)
                : Task.Factory.StartNew(() => client.PublishAll(requestDtos), token);
        }
    }
}