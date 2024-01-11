using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ServiceStack.Grpc;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack;

public abstract class GrpcServiceBase : IGrpcService
{
    private ServiceStackHost appHost;
    private ServiceStackHost AppHost => appHost ??= HostContext.AppHost;

    private RpcGateway rpcGateway;
    protected RpcGateway RpcGateway => rpcGateway ??= HostContext.AppHost.RpcGateway;

    private GrpcFeature? feature;
    protected GrpcFeature Feature => feature ??= HostContext.AssertPlugin<GrpcFeature>();

    protected async Task WriteResponseHeadersAsync(IResponse httpRes, CallContext context)
    {
        var res = (GrpcResponse) httpRes;
        var nonSuccessStatus = res.StatusCode >= 300;
        if (!Feature.DisableResponseHeaders || nonSuccessStatus)
        {
            foreach (var header in Feature.IgnoreResponseHeaders.Safe())
            {
                res.Headers.Remove(header);
            }

            if (res.Headers.Count > 0 || nonSuccessStatus)
            {
                var headers = new global::Grpc.Core.Metadata();
                if (nonSuccessStatus)
                    headers.Add(Keywords.HttpStatus, res.StatusCode.ToString());

                foreach (var entry in res.Headers)
                {
                    headers.Add(entry.Key, entry.Value);
                }

                if (nonSuccessStatus)
                {
                    var status = res.Dto.GetResponseStatus();
                    if (status != null)
                        headers.Add(Keywords.GrpcResponseStatus,
                            GrpcMarshaller<ResponseStatus>.Instance.Serializer(status));

                    var desc = status?.ErrorCode ?? res.StatusDescription ??
                        status?.Message ?? HttpStatus.GetStatusDescription(res.StatusCode);
                    context.ServerCallContext!.Status = Feature.ToGrpcStatus?.Invoke(httpRes) ?? ToGrpcStatus(res.StatusCode, desc);
                }

                await context.ServerCallContext!.WriteResponseHeadersAsync(headers).ConfigAwait();
            }
        }
    }

    protected virtual Task<TResponse> ExecuteDynamic<TRequest,TResponse>(string method, DynamicRequest request, CallContext context)
    {
        var requestType = typeof(TRequest);
        AppHost.AssertFeatures(ServiceStack.Feature.Grpc);
        var to = request.Params.ToObjectDictionary();
        var typedRequest = to?.FromObjectDictionary(requestType) ?? requestType.CreateInstance();
        if (request.Params != null)
        {
            foreach (var entry in request.Params)
            {
                context.RequestHeaders?.Add("query." + entry.Key, entry.Value);
            }
        }
        return Execute<TResponse>(method, typedRequest, context);
    }
        
    protected virtual async Task<TResponse> Execute<TResponse>(string method, object request, CallContext context)
    {
        AppHost.AssertFeatures(ServiceStack.Feature.Grpc);
        if (!Feature.DisableRequestParamsInHeaders)
            PopulateRequestFromHeaders(request, context.CallOptions.Headers);

        var req = new GrpcRequest(context, request, method);
        using var scope = req.StartScope();
        var ret = await RpcGateway.ExecuteAsync<TResponse>(request, req).ConfigAwait();
        req.Response.Dto ??= ret;
        await WriteResponseHeadersAsync(req.Response, context).ConfigAwait();
        return ret;
    }

    protected virtual void PopulateRequestFromHeaders(object request, global::Grpc.Core.Metadata headers)
    {
        if (headers.Count == 0)
            return;

        var props = TypeProperties.Get(request.GetType());
        var to = new Dictionary<string, object>();
        foreach (var entry in headers)
        {
            var key = entry.Key.IndexOf('.') >= 0 && (
                entry.Key.StartsWith("query.") ||
                entry.Key.StartsWith("form.") ||
                entry.Key.StartsWith("cookie.") ||
                entry.Key.StartsWith("header."))
                ? entry.Key.RightPart('.')
                : entry.Key;
                
            if (!props.PropertyMap.TryGetValue(key, out var accessor))
                continue;

            var propName = accessor.PropertyInfo.Name; 
            to[propName] = !entry.Key.EndsWith("-bin")
                ? entry.Value
                : entry.ValueBytes;
        }

        if (to.Count > 0)
            to.PopulateInstance(request);
    }

    protected virtual async IAsyncEnumerable<TResponse> Stream<TRequest,TResponse>(TRequest request, CallContext context)
    {
        AppHost.AssertFeatures(ServiceStack.Feature.Grpc);
        if (!Feature.DisableRequestParamsInHeaders)
            PopulateRequestFromHeaders(request, context.CallOptions.Headers);
            
        if (!Feature.RequestServiceTypeMap.TryGetValue(typeof(TRequest), out var serviceType))
            throw new NotSupportedException($"'{typeof(TRequest).Name}' was not registered in GrpcFeature.RegisterServices");

        var service = (IStreamService<TRequest,TResponse>) AppHost.Container.Resolve(serviceType);
        using var disposableService = service as IDisposable;
            
        var req = new GrpcRequest(context, request, HttpMethods.Post);
        using var scope = req.StartScope();
        var res = req.Response;

        if (service is IRequiresRequest requiresRequest)
            requiresRequest.Request = req;
            
        IAsyncEnumerable<TResponse>? response = default;
        try
        {
            if (AppHost.ApplyPreRequestFilters(req, req.Response))
                yield break;

            await AppHost.ApplyRequestFiltersAsync(req, res, request).ConfigAwait();
            if (res.IsClosed)
                yield break;

            response = service.Stream(request, context.CancellationToken);
        }
        catch (Exception e)
        {
            res.Dto = RpcGateway.CreateErrorResponse<TResponse>(res, e);
            await WriteResponseHeadersAsync(res, context).ConfigAwait();
            yield break; //written in headers
        }

        if (response != null)
        {
            var enumerator = response.GetAsyncEnumerator();
            bool more;
            try
            {
                more = await enumerator.MoveNextAsync();
            }
            catch (Exception e)
            {
                // catch + handle first Exception
                res.Dto = RpcGateway.CreateErrorResponse<TResponse>(res, e);
                await WriteResponseHeadersAsync(res, context).ConfigAwait();
                await enumerator.DisposeAsync();
                yield break; //written in headers
            }
            yield return enumerator.Current;

            while (more)
            {
                try
                {
                    more = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    await enumerator.DisposeAsync();
                    yield break;
                }
                catch (Exception)
                {
                    await enumerator.DisposeAsync();
                    yield break;
                }
                if (more)
                    yield return enumerator.Current;
            }

            await enumerator.DisposeAsync();
        }
    }

    protected virtual async IAsyncEnumerable<TResponse> StreamService<TRequest,TResponse>(IStreamService<TRequest,TResponse> service,
        TRequest request, [EnumeratorCancellation] CancellationToken cancel)
    {
        var response = service.Stream(request, cancel);
        await foreach (var item in response.WithCancellation(cancel))
        {
            yield return item;
        }
    }

    protected Status ToGrpcStatus(int httpStatus, string detail)
    {
        switch (httpStatus)
        {
            case 400:
                return new Status(StatusCode.Internal, detail); 
            case 401:
                return new Status(StatusCode.Unauthenticated, detail); 
            case 403:
                return new Status(StatusCode.PermissionDenied, detail); 
            case 404:
                return new Status(StatusCode.NotFound, detail); 
            case 409:
                return new Status(StatusCode.AlreadyExists, detail); 
            case 429:
            case 502:
            case 503:
            case 504:
                return new Status(StatusCode.Unavailable, detail); 
            default:
                return new Status(StatusCode.Unknown, detail); 
        }
    }
}