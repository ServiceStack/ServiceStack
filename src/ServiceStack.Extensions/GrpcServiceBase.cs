using System;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ServiceStack
{
    public abstract class GrpcServiceBase : IGrpcService
    {
        private RpcGateway rpcGateway;
        protected RpcGateway RpcGateway => rpcGateway ?? (rpcGateway = HostContext.AppHost.RpcGateway);

        private GrpcFeature feature;
        protected GrpcFeature Feature => feature ?? (feature = HostContext.AssertPlugin<GrpcFeature>()); 

        protected virtual async Task<TResponse> Execute<TResponse>(string method, object request, CallContext context)
        {
            var req = new GrpcRequest(context, request, method);
            var ret = await RpcGateway.ExecuteAsync<TResponse>(request, req);

            var res = (GrpcResponse) req.Response;

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
                        try
                        {
                            if (status != null)
                                headers.Add(Keywords.GrpcResponseStatus, GrpcMarshaller<ResponseStatus>.Instance.Serializer(status));
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                        
                        var desc = status?.ErrorCode ?? res.StatusDescription ?? status?.Message ?? HttpStatus.GetStatusDescription(res.StatusCode);
                        context.ServerCallContext.Status = ToGrpcStatus(res.StatusCode, desc);
                    }
                    
                    await context.ServerCallContext.WriteResponseHeadersAsync(headers);
                }

            }
            return ret;
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