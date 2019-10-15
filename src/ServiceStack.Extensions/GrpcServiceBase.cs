using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract class GrpcServiceBase : IGrpcService
    {
        private ServiceStackHost appHost;
        private ServiceStackHost AppHost => appHost ??= HostContext.AppHost;

        private RpcGateway rpcGateway;
        protected RpcGateway RpcGateway => rpcGateway ??= HostContext.AppHost.RpcGateway;

        private GrpcFeature feature;
        protected GrpcFeature Feature => feature ??= HostContext.AssertPlugin<GrpcFeature>();

        protected async Task WriteResponseHeadersAsync(IResponse httpRes, CallContext context)
        {
            var res = (GrpcResponse) httpRes;
            var nonSuccessStatus = res.StatusCode >= 300;
            if (!Feature.DisableResponseHeaders || nonSuccessStatus)
            {
                foreach (var header in Feature.IgnoreResponseHeaders)
                {
                    res.Headers.Remove(header);
                }

                if (res.Headers.Count > 0 || nonSuccessStatus)
                {
                    var headers = new Grpc.Core.Metadata();
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
                        context.ServerCallContext.Status = ToGrpcStatus(res.StatusCode, desc);
                    }

                    await context.ServerCallContext.WriteResponseHeadersAsync(headers);
                }
            }
        }

        protected virtual async Task<TResponse> Execute<TResponse>(string method, object request, CallContext context)
        {
            AppHost.AssertFeatures(ServiceStack.Feature.Grpc);

            var req = new GrpcRequest(context, request, method);
            var ret = await RpcGateway.ExecuteAsync<TResponse>(request, req);
            await WriteResponseHeadersAsync(req.Response, context);
            return ret;
        }

        protected virtual async IAsyncEnumerable<TResponse> Stream<TRequest,TResponse>(TRequest request, CallContext context)
        {
            AppHost.AssertFeatures(ServiceStack.Feature.Grpc);
            
            if (!Feature.RequestServiceTypeMap.TryGetValue(typeof(TRequest), out var serviceType))
                throw new NotSupportedException($"'{typeof(TRequest).Name}' was not registered in GrpcFeature.RegisterServices");

            var service = (IStreamService<TRequest,TResponse>) AppHost.Container.Resolve(serviceType);
            using var disposableService = service as IDisposable;
            
            var req = new GrpcRequest(context, request, HttpMethods.Post);
            var res = req.Response;

            if (service is IRequiresRequest requiresRequest)
                requiresRequest.Request = req;
            
            IAsyncEnumerable<TResponse> response = default;
            try
            {
                if (AppHost.ApplyPreRequestFilters(req, req.Response))
                    yield break;

                await AppHost.ApplyRequestFiltersAsync(req, res, request);
                if (res.IsClosed)
                    yield break;

                response = service.Stream(request, context.CancellationToken);
            }
            catch (Exception e)
            {
                res.Dto = RpcGateway.CreateErrorResponse<TResponse>(res, e);
                await WriteResponseHeadersAsync(res, context);
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
                    await WriteResponseHeadersAsync(res, context);
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
                    catch (Exception)
                    {
                        await enumerator.DisposeAsync();
                        yield break; //written in headers
                    }
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
}