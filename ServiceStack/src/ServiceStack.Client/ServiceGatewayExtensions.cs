using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack;
public static class ServiceGatewayExtensions
{
    public static ApiResult<TResponse> Api<TResponse>(this IServiceGateway client, IReturn<TResponse> request)
    {
        try
        {
            return new ApiResult<TResponse>(client.Send(request));
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    public static TResponse Send<TResponse>(this IServiceGateway client, IReturn<TResponse> request)
    {
        return client.Send<TResponse>(request);
    }

    public static ApiResult<EmptyResponse> Api(this IServiceGateway client, IReturnVoid request)
    {
        try
        {
            client.Send<byte[]>(request);
            return ApiResult.Create(new EmptyResponse());
        }
        catch (Exception ex)
        {
            return ex.ToApiResult();
        }
    }

    public static void Send(this IServiceGateway client, IReturnVoid request)
    {
        client.Send<byte[]>(request);
    }

    public static ApiResult<List<TResponse>> ApiAll<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> request)
    {
        try
        {
            return ApiResult.Create(client.SendAll<TResponse>(request));
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<List<TResponse>>();
        }
    }

    public static List<TResponse> SendAll<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> request)
    {
        return client.SendAll<TResponse>(request);
    }

    public static object Send(this IServiceGateway client, Type responseType, object request)
    {
        if (!LateBoundSendSyncFns.TryGetValue(responseType, out var sendFn))
        {
            var mi = typeof(ServiceGatewayExtensions).GetStaticMethod("SendObject");
            var genericMi = mi.MakeGenericMethod(responseType);
            sendFn = (Func<IServiceGateway, object, object>)
                genericMi.CreateDelegate(typeof(Func<IServiceGateway, object, object>));

            Dictionary<Type, Func<IServiceGateway, object, object>> snapshot, newCache;
            do
            {
                snapshot = LateBoundSendSyncFns;
                newCache = new Dictionary<Type, Func<IServiceGateway, object, object>>(LateBoundSendSyncFns)
                {
                    [responseType] = sendFn
                };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref LateBoundSendSyncFns, newCache, snapshot), snapshot));
        }
        return sendFn(client, request);
    }

    public static Task<object> SendAsync(this IServiceGateway client, Type responseType, object request, CancellationToken token = default)
    {
        if (!LateBoundSendAsyncFns.TryGetValue(responseType, out var sendFn))
        {
            var mi = typeof(ServiceGatewayExtensions).GetStaticMethod("SendObjectAsync");
            var genericMi = mi.MakeGenericMethod(responseType);
            sendFn = (Func<IServiceGateway, object, CancellationToken, Task<object>>)
                genericMi.CreateDelegate(typeof(Func<IServiceGateway, object, CancellationToken, Task<object>>));

            Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>> snapshot, newCache;
            do
            {
                snapshot = LateBoundSendAsyncFns;
                newCache = new Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>>(LateBoundSendAsyncFns) {
                    [responseType] = sendFn
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

        var responseType = returnTypeDef.GetGenericArguments()[0];
        return responseType;
    }

    private static Dictionary<Type, Func<IServiceGateway, object, object>> LateBoundSendSyncFns = new();

    internal static object SendObject<TResponse>(IServiceGateway client, object request)
    {
        return client.Send<TResponse>(request);
    }

    private static Dictionary<Type, Func<IServiceGateway, object, CancellationToken, Task<object>>> LateBoundSendAsyncFns = new();

    internal static async Task<object> SendObjectAsync<TResponse>(IServiceGateway client, object request, CancellationToken token)
    {
        return await client.SendAsync<TResponse>(request, token);
    }
}

// Needed to use Send/SendAll to avoid ambiguous signatures in IServiceClient APIs which implement both interfaces
public static class ServiceGatewayAsyncWrappers
{
    public static async Task<ApiResult<TResponse>> ApiAsync<TResponse>(this IServiceGateway client, IReturn<TResponse> requestDto, CancellationToken token = default)
    {
        try
        {
            return ApiResult.Create(await client.SendAsync<TResponse>((object)requestDto, token).ConfigAwait());
        }
        catch (Exception e)
        {
            return e.ToApiResult<TResponse>();
        }
    }

    public static Task<TResponse> SendAsync<TResponse>(this IServiceGateway client, IReturn<TResponse> requestDto, CancellationToken token = default)
    {
        return client.SendAsync<TResponse>((object)requestDto, token);
    }

    public static async Task<ApiResult<TResponse>> ApiAsync<TResponse>(this IServiceGateway client, object requestDto, CancellationToken token = default)
    {
        try
        {
            ServiceClientUtils.AssertRequestDto(requestDto);
            return ApiResult.Create(await (client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.SendAsync<TResponse>(requestDto, token)
                : Task.Factory.StartNew(() => client.Send<TResponse>(requestDto), token)).ConfigAwait());
        }
        catch (Exception e)
        {
            return e.ToApiResult<TResponse>();
        }
    }

    public static Task<TResponse> SendAsync<TResponse>(this IServiceGateway client, object requestDto, CancellationToken token = default)
    {
        ServiceClientUtils.AssertRequestDto(requestDto);
        return client is IServiceGatewayAsync nativeAsync
            ? nativeAsync.SendAsync<TResponse>(requestDto, token)
            : Task.Factory.StartNew(() => client.Send<TResponse>(requestDto), token);
    }

    public static async Task<ApiResult<EmptyResponse>> ApiAsync(this IServiceGateway client, IReturnVoid requestDto, CancellationToken token = default)
    {
        try
        {
            ServiceClientUtils.AssertRequestDto(requestDto);
            await (client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.SendAsync<byte[]>(requestDto, token)
                : Task.Factory.StartNew(() => client.Send<byte[]>(requestDto), token)).ConfigAwait();
            return ApiResult.Create(new EmptyResponse());
        }
        catch (Exception e)
        {
            return e.ToApiResult();
        }
    }

    public static Task SendAsync(this IServiceGateway client, IReturnVoid requestDto, CancellationToken token = default)
    {
        ServiceClientUtils.AssertRequestDto(requestDto);
        return client is IServiceGatewayAsync nativeAsync
            ? nativeAsync.SendAsync<byte[]>(requestDto, token)
            : Task.Factory.StartNew(() => client.Send<byte[]>(requestDto), token);
    }

    public static async Task<ApiResult<List<TResponse>>> ApiAllAsync<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token = default)
    {
        try
        {
            return ApiResult.Create(await (client is IServiceGatewayAsync nativeAsync
                ? nativeAsync.SendAllAsync<TResponse>(requestDtos, token)
                : Task.Factory.StartNew(() => client.SendAll<TResponse>(requestDtos), token)).ConfigAwait());
        }
        catch (Exception e)
        {
            return e.ToApiResult<List<TResponse>>();
        }
    }

    public static Task<List<TResponse>> SendAllAsync<TResponse>(this IServiceGateway client, IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token = default)
    {
        return client is IServiceGatewayAsync nativeAsync
            ? nativeAsync.SendAllAsync<TResponse>(requestDtos, token)
            : Task.Factory.StartNew(() => client.SendAll<TResponse>(requestDtos), token);
    }

    public static Task PublishAsync(this IServiceGateway client, object requestDto, CancellationToken token = default)
    {
        ServiceClientUtils.AssertRequestDto(requestDto);
        return client is IServiceGatewayAsync nativeAsync
            ? nativeAsync.PublishAsync(requestDto, token)
            : Task.Factory.StartNew(() => client.Publish(requestDto), token);
    }

    public static Task PublishAllAsync(this IServiceGateway client, IEnumerable<object> requestDtos, CancellationToken token = default)
    {
        return client is IServiceGatewayAsync nativeAsync
            ? nativeAsync.PublishAllAsync(requestDtos, token)
            : Task.Factory.StartNew(() => client.PublishAll(requestDtos), token);
    }
    
    /* IServiceClientAsync signatures cannot match IServiceGateway APIs to make them unambiguous */

    public static async Task<ApiResult<TResponse>> Api<TResponse>(this IServiceClientAsync client, IReturn<TResponse> requestDto, CancellationToken token = default)
    {
        try
        {
            return ApiResult.Create(await client.SendAsync<TResponse>(ServiceClientUtils.AssertRequestDto(requestDto), token).ConfigAwait());
        }
        catch (Exception e)
        {
            return e.ToApiResult<TResponse>();
        }
    }

    public static Task<TResponse> Send<TResponse>(this IServiceClientAsync client, IReturn<TResponse> requestDto, CancellationToken token = default)
    {
        return client.SendAsync<TResponse>(ServiceClientUtils.AssertRequestDto(requestDto), token);
    }

    public static async Task<ApiResult<List<TResponse>>> ApiAllAsync<TResponse>(this IServiceClientAsync client, IReturn<TResponse>[] requestDtos, CancellationToken token = default)
    {
        try
        {
            return ApiResult.Create(await client.SendAllAsync<TResponse>(requestDtos, token).ConfigAwait());
        }
        catch (Exception e)
        {
            return e.ToApiResult<List<TResponse>>();
        }
    }

    public static Task<List<TResponse>> SendAllAsync<TResponse>(this IServiceClientAsync client, IReturn<TResponse>[] requestDtos, CancellationToken token = default)
    {
        return client.SendAllAsync<TResponse>(requestDtos, token);
    }

    public static async Task<ApiResult<List<TResponse>>> ApiAllAsync<TResponse>(this IServiceClientAsync client, List<IReturn<TResponse>> requestDtos, CancellationToken token = default)
    {
        try
        {
            return ApiResult.Create(await client.SendAllAsync<TResponse>(requestDtos, token).ConfigAwait());
        }
        catch (Exception e)
        {
            return e.ToApiResult<List<TResponse>>();
        }
    }

    public static Task<List<TResponse>> SendAllAsync<TResponse>(this IServiceClientAsync client, List<IReturn<TResponse>> requestDtos, CancellationToken token = default)
    {
        return client.SendAllAsync<TResponse>(requestDtos, token);
    }

    public static Task PublishAllAsync(this IServiceGatewayAsync client, IEnumerable<IReturnVoid> requestDtos, CancellationToken token = default)
    {
        return client.PublishAllAsync(requestDtos, token);
    }

#if NET6_0_OR_GREATER
    public static async Task<ApiResult<TResponse>> ApiFormAsync<TResponse>(this IServiceGatewayFormAsync client, object requestDto, System.Net.Http.MultipartFormDataContent formData, CancellationToken token = default)
    {
        try
        {
            var result = await client.SendFormAsync<TResponse>(ServiceClientUtils.AssertRequestDto(requestDto), formData, token).ConfigAwait();
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }
#endif

}