using System.Threading.Tasks;
using Grpc.Core;

namespace ServiceStack
{
    public static class GrpcUtils
    {
        public static Task<TResponse> Execute<TRequest, TResponse>(this Channel channel, TRequest request,
            string servicesName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
            => Execute<TRequest, TResponse>(new DefaultCallInvoker(channel), request, servicesName, methodName, options,
                host);

        public static async Task<TResponse> Execute<TRequest, TResponse>(this CallInvoker invoker, TRequest request,
            string servicesName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
        {
            var method = new Method<TRequest, TResponse>(MethodType.Unary, servicesName, methodName,
                GrpcMarshaller<TRequest>.Instance, GrpcMarshaller<TResponse>.Instance);
            using (var auc = invoker.AsyncUnaryCall(method, host, options, request))
            {
                return await auc.ResponseAsync;
            }
        }
    }
}