using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class GrpcServiceStack
    {
        public static CallInvoker Client(string baseUrl, GrpcClientConfig config = null) =>
            GrpcChannel.ForAddress(baseUrl).ForServiceStack(config);
        public static CallInvoker Client(GrpcChannel channel, GrpcClientConfig config = null) =>
            channel.ForServiceStack(config);

        public static CallInvoker Client(string baseUrl, X509Certificate2 cert, GrpcClientConfig config) =>
            Client(baseUrl, cert, null, config);

        public static CallInvoker Client(string baseUrl, 
            X509Certificate2 cert, 
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> serverCertificateCustomValidationCallback = null, 
            GrpcClientConfig config = null) =>
            GrpcChannel.ForAddress(baseUrl, new GrpcChannelOptions {
                HttpClient = new HttpClient(new HttpClientHandler().AddPemCertificate(cert, serverCertificateCustomValidationCallback))
            }).ForServiceStack(config);

        public static CallInvoker ForServiceStack(this GrpcChannel channel, GrpcClientConfig config = null) =>
            channel.Intercept(new ServiceStackClientInterceptor(channel, config ?? new GrpcClientConfig()));

        public static Func<byte[], object> ParseResponseStatus { get; set; } = DeserializeBuiltinResponseStatus;

        public static object DeserializeBuiltinResponseStatus(byte[] protobufBytes) =>
            GrpcMarshaller<ResponseStatus>.Instance.Deserializer(protobufBytes);
    }
    
    // Usage Info: https://github.com/grpc/grpc/pull/12613#issuecomment-412744042
    public class ServiceStackClientInterceptor : Interceptor
    {
        public GrpcClientConfig Config { get; }
        private readonly GrpcServiceClient client;
        public ServiceStackClientInterceptor(GrpcChannel channel, GrpcClientConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.Channel = channel;
            client = new GrpcServiceClient(Config);
        }

        ClientInterceptorContext<TRequest, TResponse> CreateContext<TRequest, TResponse>(
            TRequest requestDto, ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class
        {
            var authIncluded = GrpcUtils.InitRequestDto(Config, requestDto);
            var options = context.Options.Init(Config, noAuth:authIncluded);
            return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest requestDto,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            context = CreateContext(requestDto, context);

            GrpcClientConfig.GlobalRequestFilter?.Invoke(context.Options);
            Config.RequestFilter?.Invoke(context.Options);
            
            var auc = continuation(requestDto, context);

            var aucAsync = ExecAsync(auc, () => continuation(requestDto, CreateContext(requestDto, context)));
            
            var call = new AsyncUnaryCall<TResponse>(aucAsync, auc.ResponseHeadersAsync, auc.GetStatus,
                auc.GetTrailers, auc.Dispose);
            return call;
        }

        async Task<TResponse> ExecAsync<TResponse>(AsyncUnaryCall<TResponse> auc, Func<AsyncUnaryCall<TResponse>> fn)
        {
            var (response, status, headers) = await GrpcUtils.GetResponseAsync(Config, auc).ConfigAwait();

            if (status?.ErrorCode != null)
            {
                var callInvoker = Config.Channel.CreateCallInvoker();
                if (await client.RetryRequest(Config, auc.GetStatus().StatusCode, status, callInvoker).ConfigAwait())
                {
                    using var retryAuc = fn();
                    var (retryResponse, retryStatus, retryHeaders) = await GrpcUtils.GetResponseAsync(Config, retryAuc).ConfigAwait();
                    if (retryStatus?.ErrorCode == null)
                        return retryResponse;
                }
                
                throw new WebServiceException(status.Message) {
                    StatusCode = ResponseCallContext.GetHttpStatus(headers),
                    ResponseDto = response as object ?? new ServiceStack.EmptyResponse { ResponseStatus = status },
                    ResponseHeaders = GrpcUtils.ResolveHeaders(headers),
                    State = auc.GetStatus(),
                };
            }
            return response;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest requestDto, ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            var callInvoker = Config.Channel.CreateCallInvoker();
            var auc = AsyncUnaryCall(requestDto, context, (req, ctx) => {
                context = CreateContext(requestDto, context);
                return callInvoker.AsyncUnaryCall(context.Method, context.Host, context.Options, requestDto);
            });
            var ret = auc.GetAwaiter().GetResult();
            return ret;
        }
    }
    
}