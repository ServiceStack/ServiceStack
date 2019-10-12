using System.Threading.Tasks;
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
                        headers.Add(Keywords.Status, res.StatusCode.ToString());
                    
                    foreach (var entry in res.Headers)
                    {
                        headers.Add(entry.Key, entry.Value);
                    }
                    await context.ServerCallContext.WriteResponseHeadersAsync(headers);
                }
            }
            return ret;
        }
    }
}