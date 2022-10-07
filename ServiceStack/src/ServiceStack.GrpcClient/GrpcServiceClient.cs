using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Meta;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public struct ResponseCallContext
    {
        public object Response { get; }
        public Status Status { get; }
        public Metadata Headers { get; }
        public ResponseCallContext(object response, Status status, Metadata headers)
        {
            Response = response;
            Status = status;
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

        public static byte[] GetHeaderBytes(Metadata headers, string name)
        {
            foreach (var entry in headers)
            {
                if (entry.Key.EqualsIgnoreCase(name))
                    return entry.ValueBytes;
            }
            return null;
        }

        public int HttpStatus => GetHttpStatus(Headers);
        
        public static int GetHttpStatus(Metadata headers) => 
            GetHeader(headers, GrpcClientConfig.Keywords.HttpStatus)?.ToInt() ?? default;
    }


    public class GrpcServiceClient : IRestServiceClient
    {
        private const string DefaultMethod = Methods.Post;

        public GrpcClientConfig Config { get; }

        public string SessionId
        {
            get => Config.SessionId;
            set => Config.SessionId = value;
        }
        
        public string BearerToken
        {
            get => Config.BearerToken;
            set => Config.BearerToken = value;
        }
        
        public string RefreshToken
        {
            get => Config.RefreshToken;
            set => Config.RefreshToken = value;
        }
        
        public int Version
        {
            get => Config.Version;
            set => Config.Version = value;
        }

        public Action<CallContext> RequestFilter
        {
            get => Config.RequestFilter;
            set => Config.RequestFilter = value;
        }

        public Action<ResponseCallContext> ResponseFilter
        {
            get => Config.ResponseFilter;
            set => Config.ResponseFilter = value;
        }

        public GrpcServiceClient(string url) : this(GrpcChannel.ForAddress(url)) {}
        
        public GrpcServiceClient(string url, X509Certificate2 cert) 
            : this(url, cert, null) {}
        
        public GrpcServiceClient(
            string url, 
            X509Certificate2 cert, 
            Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> serverCertificateCustomValidationCallback = null) 
            : this(GrpcChannel.ForAddress(url, new GrpcChannelOptions {
                HttpClient = new HttpClient(new HttpClientHandler().AddPemCertificate(cert, serverCertificateCustomValidationCallback))
            })) {}

        public GrpcServiceClient(GrpcChannel channel) : this(new GrpcClientConfig { Channel = channel }) {}

        public GrpcServiceClient(GrpcClientConfig config)
        {
            if (config.Channel == null)
                throw new ArgumentNullException(nameof(Config.Channel));

            Config = config;
        }

        public void Dispose() => Config.Channel?.Dispose();

        public static class Methods
        {
            internal const string Get = "Get";
            internal const string Post = "Post";
            internal const string Put = "Put";
            internal const string Delete = "Delete";
            internal const string Patch = "Patch";
        }

        delegate object ExecuteInternalDelegate(CallInvoker invoker, object request,
            string serviceName, string methodName, CallOptions options, string host);

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

            public static AsyncServerStreamingCall<TResponse> GenericStream(CallInvoker invoker, TRequest request,
                string serviceName, string methodName, CallOptions options, string host)
            {
                var method = new Method<TRequest, TResponse>(MethodType.ServerStreaming, serviceName, methodName,
                    GrpcMarshaller<TRequest>.Instance, GrpcMarshaller<TResponse>.Instance);
                var auc = invoker.AsyncServerStreamingCall(method, host, options, request);
                return auc;
            }

            public static object Stream(CallInvoker invoker, object request,
                string serviceName, string methodName, CallOptions options, string host)
            {
                return GenericStream(invoker, (TRequest) request, serviceName, methodName, options, host);
            }
        }

        private readonly ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate> execFnCache = new();

        ExecuteInternalDelegate ResolveExecute<TResponse>(object requestDto)
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
            return fn;
        }

        private readonly ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate> streamFnCache = new();

        ExecuteInternalDelegate ResolveStream<TResponse>(object requestDto)
        {
            var key = new Tuple<Type, Type>(requestDto.GetType(), typeof(TResponse));
            if (!streamFnCache.TryGetValue(key, out var fn))
            {
                var type = typeof(Executor<,>).MakeGenericType(requestDto.GetType(), typeof(TResponse));
                var mi = type.GetMethod(nameof(Stream),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                fn = (ExecuteInternalDelegate) mi.CreateDelegate(typeof(ExecuteInternalDelegate));
                streamFnCache[key] = fn;
            }
            return fn;
        }

        public async Task<bool> RetryRequest(GrpcClientConfig config, StatusCode statusCode, ResponseStatus status, CallInvoker callInvoker)
        {
            if (config.RefreshToken != null && statusCode == StatusCode.Unauthenticated)
            {
                GrpcChannel newChannel = null;
                var useInvoker = callInvoker;
                if (!string.IsNullOrEmpty(config.RefreshTokenUri))
                {
                    newChannel = GrpcChannel.ForAddress(config.RefreshTokenUri);
                    useInvoker = newChannel.CreateCallInvoker();
                }
                
                var refreshRequest = new GetAccessToken {
                    RefreshToken = config.RefreshToken,
                };
                var methodName = GrpcConfig.GetServiceName(Methods.Post, nameof(GetAccessToken));
                var options = new CallOptions().Init(config, noAuth:true);

                var fn = ResolveExecute<GetAccessTokenResponse>(refreshRequest);
                using var auc = (AsyncUnaryCall<GetAccessTokenResponse>) fn(useInvoker, refreshRequest, config.ServicesName, methodName, options, null);
                var (response, refreshStatus, headers) = await GrpcUtils.GetResponseAsync(config, auc).ConfigAwait();
                using (newChannel){}

                if (refreshStatus?.ErrorCode != null)
                {
                    throw new RefreshTokenException(new WebServiceException(refreshStatus.Message) {
                        StatusCode = ResponseCallContext.GetHttpStatus(headers),
                        ResponseDto = (object) response ?? new EmptyResponse { ResponseStatus = refreshStatus },
                        ResponseHeaders = GrpcUtils.ResolveHeaders(headers),
                        State = auc.GetStatus(),
                    });
                }
                var accessToken = response?.AccessToken;
                if (string.IsNullOrEmpty(accessToken))
                    throw new RefreshTokenException("Could not retrieve new AccessToken from: " + (config.RefreshTokenUri ?? config.BaseUri));

                config.BearerToken = accessToken;
                return true;
            }
            return false;
        }

        public async Task<TResponse> Execute<TResponse>(object requestDto, string methodName, CancellationToken token = default)
        {
            if (requestDto == null)
                throw new ArgumentNullException(nameof(requestDto));

            try 
            {
                var authIncluded = GrpcUtils.InitRequestDto(Config, requestDto);
                var options = new CallOptions().Init(Config, noAuth:authIncluded);

                GrpcClientConfig.GlobalRequestFilter?.Invoke(options);
                Config.RequestFilter?.Invoke(options);

                var callInvoker = Config.Channel.CreateCallInvoker();
                var fn = ResolveExecute<TResponse>(requestDto);
                using var auc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null);

                var (response, status, headers) = await GrpcUtils.GetResponseAsync(Config, auc).ConfigAwait();

                if (status?.ErrorCode != null)
                {
                    if (await RetryRequest(Config, auc.GetStatus().StatusCode, status, callInvoker).ConfigAwait())
                    {
                        options = new CallOptions().Init(Config, noAuth:authIncluded);
                        using var retryAuc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null);
                        var (retryResponse, retryStatus, retryHeaders) = await GrpcUtils.GetResponseAsync(Config, retryAuc).ConfigAwait();
                        if (retryStatus?.ErrorCode == null)
                            return retryResponse;
                    }
                
                    throw new WebServiceException(status.Message) {
                        StatusCode = ResponseCallContext.GetHttpStatus(headers),
                        ResponseDto = response as object ?? new EmptyResponse { ResponseStatus = status },
                        ResponseHeaders = GrpcUtils.ResolveHeaders(headers),
                        State = auc.GetStatus(),
                    };
                }

                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<TResponse>> ExecuteAll<TResponse>(object[] requestDtos,
            CancellationToken token = default)
        {
            if (requestDtos == null || requestDtos.Length == 0)
                return TypeConstants<TResponse>.EmptyList;

            this.PopulateRequestMetadatas(requestDtos);
            var firstDto = requestDtos[0];
            var methodName = GrpcConfig.GetServiceName(GetMethod(firstDto), firstDto.GetType().Name);

            this.PopulateRequestMetadatas(requestDtos);
            var authIncluded = GrpcUtils.InitRequestDto(Config, firstDto);

            var fn = ResolveExecute<TResponse>(firstDto);
            var options = new CallOptions().Init(Config, noAuth:authIncluded);

            GrpcClientConfig.GlobalRequestFilter?.Invoke(options);
            Config.RequestFilter?.Invoke(options);

            var responses = new List<TResponse>();

            var callInvoker = Config.Channel.CreateCallInvoker();

            // Handle retry on first request
            var requestDto = requestDtos[0];
            using var auc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null);
            var (response, status, headers) = await GrpcUtils.GetResponseAsync(Config, auc).ConfigAwait();

            if (status?.ErrorCode != null)
            {
                if (await RetryRequest(Config, auc.GetStatus().StatusCode, status, callInvoker).ConfigAwait())
                {
                    authIncluded = GrpcUtils.InitRequestDto(Config, requestDto);
                    fn = ResolveExecute<TResponse>(requestDto);
                    options = new CallOptions().Init(Config, noAuth:authIncluded);
                    using var retryAuc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null);

                    var (retryResponse, retryStatus, retryHeaders) = await GrpcUtils.GetResponseAsync(Config, retryAuc).ConfigAwait();
                    if (retryResponse.GetResponseStatus()?.ErrorCode == null)
                    {
                        responses.Add(retryResponse);
                    }
                }

                if (responses.Count == 0)
                {
                    throw new WebServiceException(status.Message) {
                        StatusCode = ResponseCallContext.GetHttpStatus(headers),
                        ResponseDto = response as object ?? new EmptyResponse { ResponseStatus = status },
                        ResponseHeaders = GrpcUtils.ResolveHeaders(headers),
                        State = auc.GetStatus(),
                    };
                }
            }
            else
            {
                responses.Add(response);
            }

            var asyncTasks = new List<Task<(TResponse, ResponseStatus, Metadata)>>();
            for (var i = 1; i < requestDtos.Length; i++)
            {
                requestDto = requestDtos[i];
                asyncTasks.Add(GrpcUtils.GetResponseAsync(Config, (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null)));
            }

            await Task.WhenAll(asyncTasks).ConfigAwait();

            foreach (var task in asyncTasks)
            {
                (response, _, _) = await task.ConfigAwait();
                responses.Add(response);
            }

            return responses;
        }

        public async IAsyncEnumerable<TResponse> Stream<TResponse>(object requestDto, string methodName, [EnumeratorCancellation] CancellationToken token = default)
        {
            var authIncluded = GrpcUtils.InitRequestDto(Config, requestDto);
            var fn = ResolveStream<TResponse>(requestDto);
            var options = new CallOptions().Init(Config, noAuth:authIncluded);

            GrpcClientConfig.GlobalRequestFilter?.Invoke(options);
            Config.RequestFilter?.Invoke(options);

            var callInvoker = Config.Channel.CreateCallInvoker();
            using var assc = (AsyncServerStreamingCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null);

            var (response, status, headers) = await GrpcUtils.GetResponseAsync(Config, assc).ConfigAwait();

            if (status?.ErrorCode != null)
            {
                if (await RetryRequest(Config, assc.GetStatus().StatusCode, status, callInvoker).ConfigAwait())
                {
                    fn = ResolveStream<TResponse>(requestDto);
                    using var retryAssc = (AsyncServerStreamingCall<TResponse>) fn(callInvoker, requestDto, Config.ServicesName, methodName, options, null);
                    var (retryResponse, retryStatus, retryHeaders) = await GrpcUtils.GetResponseAsync(Config, retryAssc).ConfigAwait();
                    if (retryStatus?.ErrorCode == null)
                    {
                        await foreach(var item in retryResponse.ReadAllAsync(token))
                        {
                            yield return item;
                        }
                    }
                }
                
                throw new WebServiceException(status.Message) {
                    StatusCode = ResponseCallContext.GetHttpStatus(headers),
                    ResponseDto = response as object ?? new EmptyResponse { ResponseStatus = status },
                    ResponseHeaders = GrpcUtils.ResolveHeaders(headers),
                    State = assc.GetStatus(),
                };
            }
            
            var enumerator = response.ReadAllAsync(token).GetAsyncEnumerator(token);
            try
            {
                var more = true;
                while (more)
                {
                    TResponse item;
                    try 
                    { 
                        more = await enumerator.MoveNextAsync();
                        item = enumerator.Current;
                    }
                    catch (RpcException ex)
                    {
                        status = GrpcUtils.HandleRpcException(headers, ex);
                        throw new WebServiceException(status.Message) {
                            StatusCode = ResponseCallContext.GetHttpStatus(headers),
                            ResponseDto = new EmptyResponse { ResponseStatus = status },
                            ResponseHeaders = GrpcUtils.ResolveHeaders(headers),
                            State = assc.GetStatus(),
                        };
                    }
                    if (more)
                        yield return item;
                }
            }
            finally
            {
                await enumerator.DisposeAsync(); // omitted, along with the try/finally, if the enumerator doesn't expose DisposeAsync
            }
        }
        
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Stream<TResponse>(requestDto, GrpcConfig.GetServerStreamServiceName(requestDto.GetType().Name), token);
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

            if (request is IQuery)
                return HttpMethods.Get;
            if (request is ICrud)
                return ToHttpMethod(request.GetType()) ?? DefaultMethod;
                
            return DefaultMethod;
        }
        
        static string ToHttpMethod(Type requestType)
        {
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(ICreateDb<>)))
                return HttpMethods.Post;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(IUpdateDb<>)))
                return HttpMethods.Put;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(IDeleteDb<>)))
                return HttpMethods.Delete;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(IPatchDb<>)))
                return HttpMethods.Patch;
            if (requestType.IsOrHasGenericInterfaceTypeOf(typeof(ISaveDb<>)))
                return HttpMethods.Post;
            if (typeof(IQuery).IsAssignableFrom(requestType))
                return HttpMethods.Get;

            return null;
        }

        string GetMethodName(string verb, object requestDto) => GrpcConfig.GetServiceName(verb, requestDto.GetType().Name); 

        public Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(GetMethod(requestDto), requestDto), token);
        }

        public Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(GetMethod(requestDto), requestDto), token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requestDtos, CancellationToken token = default)
        {
            return ExecuteAll<TResponse>(requestDtos?.ToArray(), token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requestDtos, CancellationToken token = default)
        {
            return ExecuteAll<TResponse>(requestDtos?.ToArray(), token);
        }

        private static void AssertPublishType(Type requestType)
        {
            if (!typeof(IReturnVoid).IsAssignableFrom(requestType) && !typeof(IReturn<EmptyResponse>).IsAssignableFrom(requestType))
                throw new NotSupportedException($"{requestType.Name} must implement IReturnVoid. Use Send* APIs for IReturn<T> DTOs");
        }

        public async Task PublishAsync(object requestDto, CancellationToken token = default)
        {
            AssertPublishType(requestDto.GetType());
            await Execute<EmptyResponse>(requestDto, GetMethodName(GetMethod(requestDto), requestDto), token).ConfigAwait();
        }

        public Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token = default)
        {
            var array = requestDtos?.ToArray();
            if (array == null || array.Length == 0)
                return Task.CompletedTask;
            AssertPublishType(array[0].GetType());
            
            return ExecuteAll<EmptyResponse>(array, token);
        }

        public void SetCredentials(string userName, string password)
        {
            Config.UserName = userName;
            Config.Password = password;
        }

        public TResponse Get<TResponse>(IReturn<TResponse> requestDto) => GetAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Get<TResponse>(object requestDto) => GetAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public void Get(IReturnVoid requestDto) => GetAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Delete<TResponse>(IReturn<TResponse> requestDto) => DeleteAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Delete<TResponse>(object requestDto) => DeleteAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public void Delete(IReturnVoid requestDto) => DeleteAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Post<TResponse>(IReturn<TResponse> requestDto) => PostAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Post<TResponse>(object requestDto) => PostAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public void Post(IReturnVoid requestDto) => PostAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Put<TResponse>(IReturn<TResponse> requestDto) => PutAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Put<TResponse>(object requestDto) => PutAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public void Put(IReturnVoid requestDto) => PutAsync(requestDto).GetAwaiter().GetResult();
        public TResponse Patch<TResponse>(IReturn<TResponse> requestDto) => PatchAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Patch<TResponse>(object requestDto) => PatchAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public void Patch(IReturnVoid requestDto) => PatchAsync(requestDto).GetAwaiter().GetResult();

        public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto) =>
            CustomMethodAsync(httpVerb, requestDto).GetAwaiter().GetResult();

        public TResponse CustomMethod<TResponse>(string httpVerb, object requestDto) =>
            CustomMethodAsync<TResponse>(httpVerb, requestDto).GetAwaiter().GetResult();

        public void CustomMethod(string httpVerb, IReturnVoid requestDto) =>
            CustomMethodAsync(httpVerb, requestDto).GetAwaiter().GetResult();

        
        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Get, requestDto), token);
        }

        public Task<TResponse> GetAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Get, requestDto), token);
        }

        public async Task GetAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Get, requestDto), token).ConfigAwait();
        }

        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Delete, requestDto), token);
        }

        public Task<TResponse> DeleteAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Delete, requestDto), token);
        }

        public async Task DeleteAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Delete, requestDto), token).ConfigAwait();
        }

        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Post, requestDto), token);
        }

        public Task<TResponse> PostAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Post, requestDto), token);
        }

        public async Task PostAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Post, requestDto), token).ConfigAwait();
        }

        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Put, requestDto), token);
        }

        public Task<TResponse> PutAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Put, requestDto), token);
        }

        public async Task PutAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Put, requestDto), token).ConfigAwait();
        }

        public Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Patch, requestDto), token);
        }

        public Task<TResponse> PatchAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Patch, requestDto), token);
        }

        public async Task PatchAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Patch, requestDto), token).ConfigAwait();
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(httpVerb, requestDto), token);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto, CancellationToken token = default)
        {
            return Execute<TResponse>(requestDto, GetMethodName(httpVerb, requestDto), token);
        }

        public async Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto, CancellationToken token = default)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(httpVerb, requestDto), token).ConfigAwait();
        }

        public TResponse Send<TResponse>(object requestDto) => SendAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public List<TResponse> SendAll<TResponse>(IEnumerable<object> requestDtos) => SendAllAsync<TResponse>(requestDtos).GetAwaiter().GetResult();

        public void Publish(object requestDto) => PublishAsync(requestDto).GetAwaiter().GetResult();

        public void PublishAll(IEnumerable<object> requestDtos) => PublishAllAsync(requestDtos).GetAwaiter().GetResult();

        internal static bool IsRequestDto(Type type)
        {
            // check to see if this is a request DTO that needs flattening over gRPC
            // i.e. inherits (directly or indirectly) from IReturn{Void|<T>}
            return type != null && !type.IsAbstract && typeof(IReturn).IsAssignableFrom(type)
                && type.GetInterfaces().Any(iType => iType == typeof(IReturnVoid)
                   || iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(IReturn<>));
        }
    }
    
    public static class MetaTypeConfig
    {
    }
    
    public class MetaTypeConfig<T>
    {
        public static MetaTypeConfig<T> Instance { get; set; } = new MetaTypeConfig<T>();

        public static MetaType metaType;

        //https://developers.google.com/protocol-buffers/docs/proto
        //The smallest field number you can specify is 1, and the largest is 2^29-1 or 536,870,911. 
        private const int MaxFieldId = 536870911; // 2^29-1

        static MetaTypeConfig()
        {
            // https://github.com/protobuf-net/protobuf-net/wiki/Getting-Started#inheritance
            var baseType = typeof(T).BaseType;
            if (!GrpcServiceClient.IsRequestDto(typeof(T)))
            {
                if (baseType != typeof(object)) 
                    RegisterSubType(typeof(T));

                // find all other sub-types in the same assembly, and eagerly register them
                var allTypes = typeof(T).Assembly.GetTypes(); // TODO: cache this?
                Type[] typeArgs = new Type[1];
                foreach(var subType in allTypes)
                {
                    if (subType.BaseType == typeof(T))
                    {
                        // touch MetaTypeConfig<subType>.Instance to force it to register if not already
                        typeArgs[0] = subType;
                        _ = typeof(MetaTypeConfig<>).MakeGenericType(typeArgs)
                            .GetProperty(nameof(Instance))?.GetValue(null);
                    }
                }
            }
            if (typeof(T).IsGenericType)
            {
                foreach (var argType in typeof(T).GenericTypeArguments)
                {
                    GrpcConfig.Register(argType);
                }
            }
        }

        private static void RegisterSubType(Type type)
        {
            if (GrpcConfig.IgnoreTypeModel(type))
                return;
            
            var baseMetaType = GrpcConfig.Register(type.BaseType);
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
        public static MetaType GetMetaType() => metaType
            ??= typeof(T).IsValueType ? null : GrpcConfig.TypeModel[typeof(T)];
    }

    public class GrpcMarshaller<T> : Marshaller<T>
    {
        public static Marshaller<T> Instance { get; set; } = new GrpcMarshaller<T>();

        static GrpcMarshaller()
        {
            //Static class is never initiated before GrpcFeature.RegisterDtoTypes() is called
            //var @break = "HERE";
        }

        public GrpcMarshaller() : base(Serialize, Deserialize) {}

        public static byte[] Serialize(T payload)
        {
            try
            {
                using var ms = new MemoryStream();
                GrpcConfig.TypeModel.Serialize(ms, payload);
                return ms.ToArray();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T Deserialize(byte[] payload)
        {
            try
            {
                using var ms = new MemoryStream(payload);
                return (T) GrpcConfig.TypeModel.Deserialize(ms, null, typeof(T));
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}