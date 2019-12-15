using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            GetHeader(headers, GrpcServiceClient.Keywords.HttpStatus)?.ToInt() ?? default;
    }

    public class GrpcServiceClient : IServiceClientAsync, IServiceClientSync, IHasSessionId, IHasBearerToken, IHasVersion
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

        public string UserAgent { get; set; } = ".NET gRPC Client " + Env.VersionString;

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
            internal const string HeaderSessionId = "X-ss-id";
            internal const string HttpStatus = "httpstatus";
            internal const string GrpcResponseStatus = "responsestatus-bin";
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

        private readonly ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate> execFnCache =
            new ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate>();

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

        private readonly ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate> streamFnCache =
            new ConcurrentDictionary<Tuple<Type, Type>, ExecuteInternalDelegate>();

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

        private CallOptions PrepareRequest(bool noAuth)
        {
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

                return new CallOptions(headers: headers);
            }
            return default;
        }

        private bool InitRequestDto(object requestDto)
        {
            this.PopulateRequestMetadata(requestDto);
            var authIncluded = !string.IsNullOrEmpty((requestDto is IHasBearerToken hasBearerToken ? hasBearerToken.BearerToken : null) ?? 
                                                     (requestDto is IHasSessionId hasSessionId ? hasSessionId.SessionId : null));
            return authIncluded;
        }

        private async Task<Metadata> InvokeResponseFilters<TResponse>(AsyncUnaryCall<TResponse> auc, TResponse response, Action<ResponseCallContext> fn = null)
        {
            var headers = await auc.ResponseHeadersAsync;
            if (GlobalResponseFilter != null || ResponseFilter != null)
            {
                var ctx = new ResponseCallContext(response, auc.GetStatus(), headers);
                fn?.Invoke(ctx);

                GlobalResponseFilter?.Invoke(ctx);
                ResponseFilter?.Invoke(ctx);
            }
            return headers;
        }

        private async Task<Metadata> InvokeResponseFilters<TResponse>(AsyncServerStreamingCall<TResponse> asc, IAsyncStreamReader<TResponse> response, Action<ResponseCallContext> fn = null)
        {
            var headers = await asc.ResponseHeadersAsync;
            if (GlobalResponseFilter != null || ResponseFilter != null)
            {
                var ctx = new ResponseCallContext(response, asc.GetStatus(), headers);
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
                if (header.Key.EndsWith("-bin"))
                    continue;
                
                to[header.Key] = header.Value;
            }
            return to;
        }

        public async Task<bool> RetryRequest(StatusCode statusCode, ResponseStatus status, CallInvoker callInvoker)
        {
            if (RefreshToken != null && statusCode == StatusCode.Unauthenticated)
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
                var methodName = GrpcConfig.GetServiceName(Methods.Post, nameof(GetAccessToken));
                var fn = ResolveExecute<GetAccessTokenResponse>(refreshRequest);
                var options = PrepareRequest(noAuth:true);

                using var auc = (AsyncUnaryCall<GetAccessTokenResponse>) fn(useInvoker, refreshRequest, ServicesName, methodName, options, null);
                var (response, refreshStatus, headers) = await GetResponse(auc);
                using (newChannel){}

                if (refreshStatus?.ErrorCode != null)
                {
                    throw new RefreshTokenException(new WebServiceException(refreshStatus.Message) {
                        StatusCode = ResponseCallContext.GetHttpStatus(headers),
                        ResponseDto = (object) response ?? new EmptyResponse { ResponseStatus = refreshStatus },
                        ResponseHeaders = ResolveHeaders(headers),
                        State = auc.GetStatus(),
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

        private async Task<(TResponse, ResponseStatus, Metadata)> GetResponse<TResponse>(AsyncUnaryCall<TResponse> auc)
        {
            var headers = await auc.ResponseHeadersAsync;
            ResponseStatus status = null;
            TResponse response = default;
            try
            {
                response = await auc.ResponseAsync;
                status = response.GetResponseStatus();
            }
            catch (RpcException ex)
            {
                var statusBytes = ResponseCallContext.GetHeaderBytes(headers, Keywords.GrpcResponseStatus);
                status = statusBytes != null 
                    ? GrpcMarshaller<ResponseStatus>.Instance.Deserializer(statusBytes)
                    : new ResponseStatus {
                        ErrorCode = ex.Status.Detail ?? ex.StatusCode.ToString(),
                        Message = HttpStatus.GetStatusDescription(ResponseCallContext.GetHttpStatus(headers)) 
                    };

                if (string.IsNullOrEmpty(status.Message))
                {
                    status.ErrorCode = ex.StatusCode.ToString();
                    status.Message = ex.Status.Detail;
                }
                
                var prop = TypeProperties<TResponse>.GetAccessor(nameof(IHasResponseStatus.ResponseStatus));
                if (prop != null)
                {
                    response = typeof(TResponse).CreateInstance<TResponse>();
                    prop.PublicSetter(response, status);
                }
            }
            finally
            {
                await InvokeResponseFilters(auc, response);
            }

            return (response, status, headers);
        }

        private async Task<(IAsyncStreamReader<TResponse>, ResponseStatus, Metadata)> GetResponse<TResponse>(AsyncServerStreamingCall<TResponse> auc)
        {
            var headers = await auc.ResponseHeadersAsync;
            ResponseStatus status = null;
            IAsyncStreamReader<TResponse> response = default;
            try
            {
                response = auc.ResponseStream;
                status = response.GetResponseStatus();
            }
            catch (RpcException ex)
            {
                status = HandleRpcException(headers, ex);
            }
            finally
            {
                await InvokeResponseFilters(auc, response);
            }

            return (response, status, headers);
        }

        private static ResponseStatus HandleRpcException(Metadata headers, RpcException ex)
        {
            var statusBytes = ResponseCallContext.GetHeaderBytes(headers, Keywords.GrpcResponseStatus);
            var status = statusBytes != null
                ? GrpcMarshaller<ResponseStatus>.Instance.Deserializer(statusBytes)
                : new ResponseStatus {
                    ErrorCode = ex.Status.Detail ?? ex.StatusCode.ToString(),
                    Message = HttpStatus.GetStatusDescription(ResponseCallContext.GetHttpStatus(headers))
                };
            return status;
        }

        public async Task<TResponse> Execute<TResponse>(object requestDto, string methodName, CancellationToken token = default)
        {
            if (requestDto == null)
                throw new ArgumentNullException(nameof(requestDto));
            
            var authIncluded = InitRequestDto(requestDto);
            var fn = ResolveExecute<TResponse>(requestDto);
            var options = PrepareRequest(noAuth:authIncluded);

            GlobalRequestFilter?.Invoke(options);
            RequestFilter?.Invoke(options);

            var callInvoker = channel.CreateCallInvoker();
            using var auc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);

            var (response, status, headers) = await GetResponse(auc);

            if (status?.ErrorCode != null)
            {
                if (await RetryRequest(auc.GetStatus().StatusCode, status, callInvoker))
                {
                    options = PrepareRequest(noAuth:authIncluded);
                    using var retryAuc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
                    var (retryResponse, retryStatus, retryHeaders) = await GetResponse(retryAuc);
                    if (retryStatus?.ErrorCode == null)
                        return retryResponse;
                }
                
                throw new WebServiceException(status.Message) {
                    StatusCode = ResponseCallContext.GetHttpStatus(headers),
                    ResponseDto = response as object ?? new EmptyResponse { ResponseStatus = status },
                    ResponseHeaders = ResolveHeaders(headers),
                    State = auc.GetStatus(),
                };
            }

            return response;
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
            var authIncluded = InitRequestDto(firstDto);

            var fn = ResolveExecute<TResponse>(firstDto);
            var options = PrepareRequest(noAuth:authIncluded);

            GlobalRequestFilter?.Invoke(options);
            RequestFilter?.Invoke(options);

            var responses = new List<TResponse>();

            var callInvoker = channel.CreateCallInvoker();

            // Handle retry on first request
            var requestDto = requestDtos[0];
            using var auc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
            var (response, status, headers) = await GetResponse(auc);

            if (status?.ErrorCode != null)
            {
                if (await RetryRequest(auc.GetStatus().StatusCode, status, callInvoker))
                {
                    authIncluded = InitRequestDto(requestDto);
                    fn = ResolveExecute<TResponse>(requestDto);
                    options = PrepareRequest(noAuth:authIncluded);
                    using var retryAuc = (AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);

                    var (retryResponse, retryStatus, retryHeaders) = await GetResponse(retryAuc);
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
                        ResponseHeaders = ResolveHeaders(headers),
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
                asyncTasks.Add(GetResponse((AsyncUnaryCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null)));
            }

            await Task.WhenAll(asyncTasks);

            foreach (var task in asyncTasks)
            {
                (response, _, _) = await task;
                responses.Add(response);
            }

            return responses;
        }

        public async IAsyncEnumerable<TResponse> Stream<TResponse>(object requestDto, string methodName, [EnumeratorCancellation] CancellationToken token = default)
        {
            var authIncluded = InitRequestDto(requestDto);
            var fn = ResolveStream<TResponse>(requestDto);
            var options = PrepareRequest(noAuth:authIncluded);

            GlobalRequestFilter?.Invoke(options);
            RequestFilter?.Invoke(options);

            var callInvoker = channel.CreateCallInvoker();
            using var assc = (AsyncServerStreamingCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);

            var (response, status, headers) = await GetResponse(assc);

            if (status?.ErrorCode != null)
            {
                if (await RetryRequest(assc.GetStatus().StatusCode, status, callInvoker))
                {
                    fn = ResolveStream<TResponse>(requestDto);
                    options = PrepareRequest(noAuth:authIncluded);
                    using var retryAssc = (AsyncServerStreamingCall<TResponse>) fn(callInvoker, requestDto, ServicesName, methodName, options, null);
                    var (retryResponse, retryStatus, retryHeaders) = await GetResponse(retryAssc);
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
                    ResponseHeaders = ResolveHeaders(headers),
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
                        status = HandleRpcException(headers, ex);
                        throw new WebServiceException(status.Message) {
                            StatusCode = ResponseCallContext.GetHttpStatus(headers),
                            ResponseDto = new EmptyResponse { ResponseStatus = status },
                            ResponseHeaders = ResolveHeaders(headers),
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

            return DefaultMethod;
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
            await Execute<EmptyResponse>(requestDto, GetMethodName(GetMethod(requestDto), requestDto), token);
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
            UserName = userName;
            Password = password;
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
        public TResponse Patch<TResponse>(IReturn<TResponse> requestDto) => PutAsync(requestDto).GetAwaiter().GetResult();

        public TResponse Patch<TResponse>(object requestDto) => PutAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public void Patch(IReturnVoid requestDto) => PutAsync(requestDto).GetAwaiter().GetResult();

        public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto) =>
            CustomMethodAsync(httpVerb, requestDto).GetAwaiter().GetResult();

        public TResponse CustomMethod<TResponse>(string httpVerb, object requestDto) =>
            CustomMethodAsync<TResponse>(httpVerb, requestDto).GetAwaiter().GetResult();

        public void CustomMethod(string httpVerb, IReturnVoid requestDto) =>
            CustomMethodAsync(httpVerb, requestDto).GetAwaiter().GetResult();

        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Get, requestDto));
        }

        public Task<TResponse> GetAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Get, requestDto));
        }

        public async Task GetAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Get, requestDto));
        }

        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Delete, requestDto));
        }

        public Task<TResponse> DeleteAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Delete, requestDto));
        }

        public async Task DeleteAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Delete, requestDto));
        }

        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Post, requestDto));
        }

        public Task<TResponse> PostAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Post, requestDto));
        }

        public async Task PostAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Post, requestDto));
        }

        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Put, requestDto));
        }

        public Task<TResponse> PutAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Put, requestDto));
        }

        public async Task PutAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Put, requestDto));
        }

        public Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Patch, requestDto));
        }

        public Task<TResponse> PatchAsync<TResponse>(object requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(Methods.Patch, requestDto));
        }

        public async Task PatchAsync(IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(Methods.Patch, requestDto));
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(httpVerb, requestDto));
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
        {
            return Execute<TResponse>(requestDto, GetMethodName(httpVerb, requestDto));
        }

        public async Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            await Execute<EmptyResponse>(requestDto, GetMethodName(httpVerb, requestDto));
        }

        public TResponse Send<TResponse>(object requestDto) => SendAsync<TResponse>(requestDto).GetAwaiter().GetResult();

        public List<TResponse> SendAll<TResponse>(IEnumerable<object> requestDtos) => SendAllAsync<TResponse>(requestDtos).GetAwaiter().GetResult();

        public void Publish(object requestDto) => PublishAsync(requestDto).GetAwaiter().GetResult();

        public void PublishAll(IEnumerable<object> requestDtos) => PublishAllAsync(requestDtos).GetAwaiter().GetResult();
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
            ??= typeof(T).IsValueType ? null : GrpcConfig.TypeModel.Add(typeof(T), applyDefaultBehaviour:true); 

        public GrpcMarshaller() : base(Serialize, Deserialize) {}

        public static byte[] Serialize(T payload)
        {
            try 
            { 
                using (var ms = new MemoryStream())
                {
                    GrpcConfig.TypeModel.Serialize(ms, payload);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static T Deserialize(byte[] payload)
        {
            try 
            { 
                using (var ms = new MemoryStream(payload))
                {
                    return (T) GrpcConfig.TypeModel.Deserialize(ms, null, typeof(T));
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}