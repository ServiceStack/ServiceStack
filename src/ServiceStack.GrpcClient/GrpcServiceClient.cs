using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Meta;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack
{
    public struct ResponseCallContext
    {
        public object Response { get; }
        public Metadata Headers { get; }
        public ResponseCallContext(object response, Metadata headers)
        {
            Response = response;
            Headers = headers ?? new Metadata();
        }

        public string GetHeader(string name) => GetHeader(Headers, name);
        public static string GetHeader(Metadata headers, string name)
        {
            foreach (var entry in headers)
            {
                if (entry.Key.EqualsIgnoreCase(name))
                    return entry.Value;
            }
            return null;
        }

        public int StatusCode => GetStatusCode(Headers);
        
        public static int GetStatusCode(Metadata headers) => 
            GetHeader(headers, GrpcServiceClient.Keywords.Status)?.ToInt() ?? default;
    }

    public class GrpcServiceClient : IServiceClientAsync, IHasSessionId, IHasBearerToken, IHasVersion
    {
        public string BaseUri { get; set; }
        public string SessionId { get; set; }
        public string BearerToken { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenUri { get; set; }
        public int Version { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string ServicesName { get; set; } = "GrpcServices";

        private const string DefaultMethod = Methods.Post;

        public static Action<CallContext> GlobalRequestFilter { get; set; }
        public Action<CallContext> RequestFilter { get; set; }
        public static Action<ResponseCallContext> GlobalResponseFilter { get; set; }
        public Action<ResponseCallContext> ResponseFilter { get; set; }

        public string UserAgent { get; set; } = ".NET gRPC Client " + Env.ServiceStackVersion;

        public Grpc.Core.Metadata Headers { get; } = new Grpc.Core.Metadata();

        private readonly GrpcChannel channel;

        public GrpcServiceClient(string url) : this(GrpcChannel.ForAddress(url))
        {
            BaseUri = url;
        }

        public GrpcServiceClient(GrpcChannel channel)
        {
            this.channel = channel;
            BaseUri = this.channel.Target;
        }

        public void Dispose() => channel.Dispose();

        static class Methods
        {
            internal const string Get = "Get";
            internal const string Post = "Post";
            internal const string Put = "Put";
            internal const string Delete = "Delete";
            internal const string Patch = "Patch";
        }

        internal static class Keywords
        {
            internal const string AutoBatchIndex = nameof(AutoBatchIndex);
            internal const string HeaderSessionId = "X-ss-id";
            internal const string Status = "status";
        }

        delegate object ExecuteInternalDelegate(CallInvoker invoker, object request,
            string serviceName, string methodName, CallOptions options, string host);

        private readonly ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate> execFnCache =
            new ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate>();

        internal class Executor<TRequest, TResponse>
            where TRequest : class
            where TResponse : class
        {
            public static AsyncUnaryCall<TResponse> GenericExecute(CallInvoker invoker, TRequest request,
                string serviceName, string methodName, CallOptions options, string host)
            {
                var method = new Method<TRequest, TResponse>(MethodType.Unary, serviceName, methodName,
                    GrpcMarshaller<TRequest>.Instance, GrpcMarshaller<TResponse>.Instance);
                var auc = invoker.AsyncUnaryCall(method, host, options, request);
                return auc;
            }

            public static object Execute(CallInvoker invoker, object request, string serviceName, string methodName, CallOptions options, string host)
            {
                return GenericExecute(invoker, (TRequest) request, serviceName, methodName, options, host);
            }
        }

        private ExecuteInternalDelegate PrepareRequest<TResponse>(object requestDto, bool noAuth, out CallOptions options)
        {
            var key = new Tuple<Type, Type>(requestDto.GetType(), typeof(TResponse));

            if (!execFnCache.TryGetValue(key, out var fn))
            {
                var type = typeof(Executor<,>).MakeGenericType(requestDto.GetType(), typeof(TResponse));
                var mi = type.GetMethod(nameof(Execute),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                fn = (ExecuteInternalDelegate) mi.CreateDelegate(typeof(ExecuteInternalDelegate));
                execFnCache[key] = fn;
            }

            options = default;
            var auth = noAuth
                ? null
                : !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password)
                    ? "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(UserName + ":" + Password))
                    : !string.IsNullOrEmpty(BearerToken)
                        ? "Bearer " + BearerToken
                        : !string.IsNullOrEmpty(SessionId)
                            ? nameof(SessionId)
                            : null;

            if (Headers.Count > 0 || auth != null || UserAgent != null)
            {
                var headers = new Grpc.Core.Metadata();
                foreach (var entry in Headers)
                {
                    headers.Add(entry);
                }

                if (auth != null)
                {
                    if (auth == nameof(SessionId))
                        headers.Add(Keywords.HeaderSessionId, SessionId);
                    else
                        headers.Add(HttpHeaders.Authorization, auth);
                }

                if (UserAgent != null)
                    headers.Add(HttpHeaders.UserAgent, UserAgent);

                options = new CallOptions(headers: headers);
            }

            return fn;
        }

        private bool InitRequestDto(object requestDto)
        {
            if (Version != default && requestDto is IHasVersion hasVersion)
                hasVersion.Version = Version;

            var authIncluded = false;
            if (!string.IsNullOrEmpty(BearerToken) && requestDto is IHasBearerToken hasBearerToken)
            {
                authIncluded = true;
                hasBearerToken.BearerToken = BearerToken;
            }

            if (!string.IsNullOrEmpty(SessionId) && requestDto is IHasSessionId hasSessionId)
            {
                authIncluded = true;
                hasSessionId.SessionId = SessionId;
            }

            return authIncluded;
        }

        private async Task<Metadata> InvokeResponseFilters<TResponse>(AsyncUnaryCall<TResponse> auc, TResponse response, Action<ResponseCallContext> fn = null)
        {
            var headers = await auc.ResponseHeadersAsync;
            if (GlobalResponseFilter != null || ResponseFilter != null)
            {
                var ctx = new ResponseCallContext(response, headers);
                fn?.Invoke(ctx);

                GlobalResponseFilter?.Invoke(ctx);
                ResponseFilter?.Invoke(ctx);
            }
            return headers;
        }

        public WebHeaderCollection ResolveHeaders(Metadata headers)
        {
            var to = new WebHeaderCollection();
            foreach (var header in headers)
            {
                to[header.Key] = header.Value;
            }
            return to;
        }

        public async Task<bool> RetryRequest(int statusCode, ResponseStatus status, CallInvoker callInvoker)
        {
            if (RefreshToken != null &&
                (statusCode == (int)HttpStatusCode.Unauthorized || status.ErrorCode == nameof(HttpStatusCode.Unauthorized)))
            {
                GrpcChannel newChannel = null;
                var useInvoker = callInvoker;
                if (!string.IsNullOrEmpty(RefreshTokenUri))
                {
                    newChannel = GrpcChannel.ForAddress(RefreshTokenUri);
                    useInvoker = newChannel.CreateCallInvoker();
                }
                
                var refreshRequest = new GetAccessToken {
                    RefreshToken = RefreshToken,
                };
                var methodName = Methods.Post + nameof(GetAccessToken);
                var fn = PrepareRequest<GetAccessTokenResponse>(refreshRequest, noAuth: true, out var options);

                using var auc = (AsyncUnaryCall<GetAccessTokenResponse>) fn(useInvoker, refreshRequest, ServicesName, methodName, options, null);
                var response = await auc.ResponseAsync;
                using (newChannel){}

                var refreshStatus = response.GetResponseStatus();
                var headers = await auc.ResponseHeadersAsync;
                if (refreshStatus?.ErrorCode != null || ResponseCallContext.GetStatusCode(headers) >= 300)
                {
                    throw new RefreshTokenException(new WebServiceException(refreshStatus.Message) {
                        StatusCode = ResponseCallContext.GetStatusCode(headers),
                        ResponseDto = response,
                        ResponseHeaders = ResolveHeaders(headers),
                    });
                }
                var accessToken = response?.AccessToken;
                if (string.IsNullOrEmpty(accessToken))
                    throw new RefreshTokenException("Could not retrieve new AccessToken from: " + (RefreshTokenUri ?? BaseUri));

                BearerToken = accessToken;
                return true;
            }
            return false;
        }

        public async Task<TResponse> Execute<TResponse>(object requestDto, string methodName, CancellationToken token = default)
        {
            var authIncluded = InitRequestDto(requestDto);
            var fn = PrepareRequest<TResponse>(requestDto, noAuth: authIncluded, out var options);

            GlobalRequestFilter?.Invoke(options);
            RequestFilter?.Invoke(options);

            var callInvoker = channel.CreateCallInvoker();
            using var auc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
            
            TResponse response;
            try
            {
                response = await auc.ResponseAsync;
            }
            catch (Exception)
            {
                throw;
            }

            var headers = await InvokeResponseFilters(auc, response);
            var statusCode = ResponseCallContext.GetStatusCode(headers);

            var status = response.GetResponseStatus();
            if (status?.ErrorCode != null || statusCode >= 300)
            {
                if (await RetryRequest(statusCode, status, callInvoker))
                {
                    authIncluded = InitRequestDto(requestDto);
                    fn = PrepareRequest<TResponse>(requestDto, noAuth: authIncluded, out options);
                    using var retryAuc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
                    var retryResponse = await retryAuc.ResponseAsync;
                    var retryHeaders = await InvokeResponseFilters(retryAuc, retryResponse);

                    if (retryResponse.GetResponseStatus()?.ErrorCode == null && ResponseCallContext.GetStatusCode(retryHeaders) < 300)
                        return retryResponse;
                }
                
                throw new WebServiceException(status.Message) {
                    StatusCode = statusCode,
                    ResponseDto = response,
                    ResponseHeaders = ResolveHeaders(headers),
                };
            }

            return response;
        }

        public async Task<List<TResponse>> ExecuteAll<TResponse>(object[] requestDtos,
            CancellationToken token = default)
        {
            if (requestDtos == null || requestDtos.Length == 0)
                return TypeConstants<TResponse>.EmptyList;

            var firstDto = requestDtos[0];
            var methodName = GetMethod(firstDto) + firstDto.GetType().Name;
            var authIncluded = InitRequestDto(firstDto);

            var fn = PrepareRequest<TResponse>(firstDto, noAuth: authIncluded, out var options);

            GlobalRequestFilter?.Invoke(options);
            RequestFilter?.Invoke(options);

            var responses = new List<TResponse>();

            var callInvoker = channel.CreateCallInvoker();
            for (var i = 0; i < requestDtos.Length; i++)
            {
                var requestDto = requestDtos[i];
                using var auc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
                var response = await auc.ResponseAsync;

                var status = response.GetResponseStatus();
                var headers = await auc.ResponseHeadersAsync;
                var statusCode = ResponseCallContext.GetStatusCode(headers);

                if (status?.ErrorCode != null || statusCode > 300)
                {
                    if (await RetryRequest(statusCode, status, callInvoker))
                    {
                        authIncluded = InitRequestDto(requestDto);
                        fn = PrepareRequest<TResponse>(requestDto, noAuth: authIncluded, out options);
                        using var retryAuc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
                        var retryResponse = await retryAuc.ResponseAsync;
                        var retryHeaders = await InvokeResponseFilters(retryAuc, retryResponse);

                        if (retryResponse.GetResponseStatus()?.ErrorCode == null && ResponseCallContext.GetStatusCode(retryHeaders) < 300)
                        {
                            responses.Add(retryResponse);
                            continue;
                        }
                    }
                    
                    await InvokeResponseFilters(auc, response, ctx => ctx.Headers.Add(Keywords.AutoBatchIndex, i.ToString()));
                    throw new WebServiceException(status.Message) {
                        StatusCode = statusCode,
                        ResponseDto = response,
                        ResponseHeaders = ResolveHeaders(headers),
                    };
                }

                responses.Add(response);
                
                if (i == requestDtos.Length - 1)
                    await InvokeResponseFilters(auc, response, ctx => ctx.Headers.Add(Keywords.AutoBatchIndex, i.ToString()));
            }

            return responses;
        }

        protected string GetMethod(object request)
        {
            if (request is IVerb)
            {
                if (request is IGet)
                    return Methods.Get;
                if (request is IPost)
                    return Methods.Post;
                if (request is IPut)
                    return Methods.Put;
                if (request is IDelete)
                    return Methods.Delete;
                if (request is IPatch)
                    return Methods.Patch;
            }

            return DefaultMethod;
        }

        public Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethod(requestDto) + requestDto.GetType().Name, token);
        }

        public Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethod(requestDto) + requestDto.GetType().Name, token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token = default)
        {
            return ExecuteAll<TResponse>(requestDtos?.ToArray(), token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requestDtos, CancellationToken token = default)
        {
            return ExecuteAll<TResponse>(requestDtos?.ToArray(), token);
        }

        public async Task PublishAsync(object requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethod(requestDto) + requestDto.GetType().Name, token);
        }

        public Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token = default)
        {
            return ExecuteAll<EmptyResponse>(requestDtos?.ToArray(), token);
        }

        public void SetCredentials(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Get + requestDto.GetType().Name);
        }

        public Task<TResponse> GetAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Get + requestDto.GetType().Name);
        }

        public async Task GetAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, Methods.Get + requestDto.GetType().Name);
        }

        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Delete + requestDto.GetType().Name);
        }

        public Task<TResponse> DeleteAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Delete + requestDto.GetType().Name);
        }

        public async Task DeleteAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, Methods.Delete + requestDto.GetType().Name);
        }

        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Post + requestDto.GetType().Name);
        }

        public Task<TResponse> PostAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Post + requestDto.GetType().Name);
        }

        public async Task PostAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, Methods.Post + requestDto.GetType().Name);
        }

        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Put + requestDto.GetType().Name);
        }

        public Task<TResponse> PutAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, Methods.Put + requestDto.GetType().Name);
        }

        public async Task PutAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, Methods.Put + requestDto.GetType().Name);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, httpVerb.ToPascalCase() + requestDto.GetType().Name);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
        {
            return Execute<TResponse>(requestDto, httpVerb.ToPascalCase() + requestDto.GetType().Name);
        }

        public async Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, httpVerb.ToPascalCase() + requestDto.GetType().Name);
        }
    }

    public class GrpcMarshaller<T> : Marshaller<T>
    {
        public static Marshaller<T> Instance { get; set; } = new GrpcMarshaller<T>();

        public static MetaType metaType;

        //https://developers.google.com/protocol-buffers/docs/proto
        //The smallest field number you can specify is 1, and the largest is 2^29-1 or 536,870,911. 
        private const int MaxFieldId = 536870911; // 2^29-1

        static GrpcMarshaller()
        {
            // https://github.com/protobuf-net/protobuf-net/wiki/Getting-Started#inheritance
            var baseType = typeof(T).BaseType;
            if (baseType != typeof(object))
            {
                RegisterSubType(typeof(T));
            }
            if (typeof(T).IsGenericType)
            {
                foreach (var argType in typeof(T).GenericTypeArguments)
                {
                    GrpcUtils.Register(argType);
                }
            }
        }

        private static void RegisterSubType(Type type)
        {
            var baseMetaType = GrpcUtils.Register(type.BaseType);
            // need to generate predictable fieldIds, allow specifying with [Id(n)] or use MurmurHash2 hash function % 2^29-1,
            var idAttr = type.FirstAttribute<IdAttribute>();
            var fieldId = idAttr?.Id ?? Math.Abs(unchecked((int) MurmurHash2.Hash(GetTypeName(type))));
            fieldId = fieldId % MaxFieldId;

            if (fieldId == default || (idAttr == null && fieldId < 50)) // min = 1, avoid hash conflicts with real field ids
                fieldId += 50;
            else if (fieldId >= 19000 && fieldId <= 19999) //cannot use the numbers 19000 through 19999
                fieldId += 1000;

            baseMetaType.AddSubType(fieldId, type);
        }

        private static string GetTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            
            var sb = StringBuilderCache.Allocate()
                .Append(type.Name.LeftPart('`'))
                .Append("<");

            for (var i = 0; i < type.GenericTypeArguments.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                
                var arg = type.GenericTypeArguments[i];
                sb.Append(GetTypeName(arg));
            }

            sb.Append('>');
            return StringBuilderCache.ReturnAndFree(sb);
        }

        //also forces static initializer
        public static MetaType GetMetaType() => metaType ?? (metaType = GrpcUtils.TypeModel.Add(typeof(T), applyDefaultBehaviour:true)); 

        public GrpcMarshaller() : base(Serialize, Deserialize) {}

        private static byte[] Serialize(T payload)
        {
            using (var ms = new MemoryStream())
            {
                GrpcUtils.TypeModel.Serialize(ms, payload);
                return ms.ToArray();
            }
        }
 
        private static T Deserialize(byte[] payload)
        {
            using (var ms = new MemoryStream(payload))
            {
                return (T) GrpcUtils.TypeModel.Deserialize(ms, null, typeof(T));
            }
        }
    }
    
    public static class GrpcUtils
    {
        public static MetaType Register<T>() => GrpcMarshaller<T>.GetMetaType();

        private static readonly ConcurrentDictionary<Type, Func<MetaType>> FnCache = new ConcurrentDictionary<Type, Func<MetaType>>();
        
        public static MetaType Register(Type type)
        {
            if (!FnCache.TryGetValue(type, out var fn))
            {
                var grpc = typeof(GrpcMarshaller<>).MakeGenericType(type);
                var mi = grpc.GetMethod("GetMetaType", BindingFlags.Static | BindingFlags.Public);
                FnCache[type] = fn = (Func<MetaType>) mi.CreateDelegate(typeof(Func<MetaType>));
            }
            return fn();
        }

        public static RuntimeTypeModel TypeModel { get; } = ProtoBuf.Meta.TypeModel.Create();
        
        public static Task<TResponse> Execute<TRequest, TResponse>(this Channel channel, TRequest request, string serviceName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
            => Execute<TRequest, TResponse>(new DefaultCallInvoker(channel), request, serviceName, methodName, options, host);
        
        public static async Task<TResponse> Execute<TRequest, TResponse>(this CallInvoker invoker, TRequest request, string serviceName, string methodName,
            CallOptions options = default, string host = null)
            where TRequest : class
            where TResponse : class
        {
            var method = new Method<TRequest, TResponse>(MethodType.Unary, serviceName, methodName,
                GrpcMarshaller<TRequest>.Instance, GrpcMarshaller<TResponse>.Instance);
            using (var auc = invoker.AsyncUnaryCall(method, host, options, request))
            {
                return await auc.ResponseAsync;
            }
        }
    }    
}