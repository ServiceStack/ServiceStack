// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack
{
    public class JsonHttpClient : IServiceClient, IJsonServiceClient, IHasCookieContainer, IServiceClientMeta
    {
        public static ILog log = LogManager.GetLogger(typeof(JsonHttpClient));

        public static Func<HttpMessageHandler> GlobalHttpMessageHandlerFactory { get; set; }
        public HttpMessageHandler HttpMessageHandler { get; set; }

        public HttpClient HttpClient { get; set; }
        public CookieContainer CookieContainer { get; set; }

        public ResultsFilterHttpDelegate ResultsFilter { get; set; }
        public ResultsFilterHttpResponseDelegate ResultsFilterResponse { get; set; }
        public ExceptionFilterHttpDelegate ExceptionFilter { get; set; }

        public const string DefaultHttpMethod = HttpMethods.Post;
        public static string DefaultUserAgent = "ServiceStack .NET HttpClient " + Env.VersionString;

        public string BaseUri { get; set; }

        public string Format { get; private set; }
        public string RequestCompressionType { get; set; }
        public string ContentType = MimeTypes.Json;

        public string SyncReplyBaseUri { get; set; }

        public string AsyncOneWayBaseUri { get; set; }

        public int Version { get; set; }
        public string SessionId { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AlwaysSendBasicAuthHeader { get; set; }

        public ICredentials Credentials { get; set; }

        public string BearerToken { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshTokenUri { get; set; }

        public bool UseTokenCookie { get; set; }

        /// <summary>
        /// Gets the collection of headers to be added to outgoing requests.
        /// </summary>
        public NameValueCollection Headers { get; private set; }

        public void SetBaseUri(string baseUri)
        {
            this.BaseUri = baseUri;
            this.SyncReplyBaseUri = baseUri.WithTrailingSlash() + Format + "/reply/";
            this.AsyncOneWayBaseUri = baseUri.WithTrailingSlash() + Format + "/oneway/";
        }

        public JsonHttpClient(string baseUri) : this()
        {
            SetBaseUri(baseUri);
        }

        public JsonHttpClient()
        {
            this.Format = "json";
            this.Headers = new NameValueCollection();
            this.CookieContainer = new CookieContainer();

            JsConfig.InitStatics();
        }

        public void SetCredentials(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        public UrlResolverDelegate UrlResolver { get; set; }

        public TypedUrlResolverDelegate TypedUrlResolver { get; set; }

        public virtual string ToAbsoluteUrl(string relativeOrAbsoluteUrl)
        {
            return relativeOrAbsoluteUrl.StartsWith("http:")
                || relativeOrAbsoluteUrl.StartsWith("https:")
                     ? relativeOrAbsoluteUrl
                     : this.BaseUri.CombineWith(relativeOrAbsoluteUrl);
        }

        public virtual string ResolveUrl(string httpMethod, string relativeOrAbsoluteUrl)
        {
            return ToAbsoluteUrl(UrlResolver?.Invoke(this, httpMethod, relativeOrAbsoluteUrl) ?? relativeOrAbsoluteUrl);
        }

        public virtual string ResolveTypedUrl(string httpMethod, object requestDto)
        {
            this.PopulateRequestMetadata(requestDto);
            return ToAbsoluteUrl(TypedUrlResolver?.Invoke(this, httpMethod, requestDto) ?? requestDto.ToUrl(httpMethod, Format));
        }

        public HttpClient GetHttpClient()
        {
            //Should reuse same instance: http://social.msdn.microsoft.com/Forums/en-US/netfxnetcom/thread/4e12d8e2-e0bf-4654-ac85-3d49b07b50af/
            if (HttpClient != null)
                return HttpClient;

            if (HttpMessageHandler == null && GlobalHttpMessageHandlerFactory != null)
                HttpMessageHandler = GlobalHttpMessageHandlerFactory();

            var handler = HttpMessageHandler ?? new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = CookieContainer,
                UseDefaultCredentials = Credentials == null,
                Credentials = Credentials,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            var baseUri = BaseUri != null ? new Uri(BaseUri) : null;

            var client = new HttpClient(handler) { BaseAddress = baseUri };

            if (BearerToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
            else if (AlwaysSendBasicAuthHeader)
                AddBasicAuth(client);

            return HttpClient = client;
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }

        private int activeAsyncRequests = 0;

        internal sealed class GzipContent : HttpContent
        {
            private readonly HttpContent content;
            public GzipContent(HttpContent content)
            {
                this.content = content;
                foreach (var header in content.Headers)
                {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                Headers.ContentEncoding.Add(CompressionTypes.GZip);
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                using (var zip = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    await content.CopyToAsync(zip);
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }
        internal sealed class DeflateContent : HttpContent
        {
            private readonly HttpContent content;
            public DeflateContent(HttpContent content)
            {
                this.content = content;
                foreach (var header in content.Headers)
                {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                Headers.ContentEncoding.Add(CompressionTypes.Deflate);
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                using (var zip = new DeflateStream(stream, CompressionMode.Compress, true))
                {
                    await content.CopyToAsync(zip);
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = -1;
                return false;
            }
        }

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default)
        {
            var client = GetHttpClient();

            if (!HttpUtils.HasRequestBody(httpMethod) && request != null)
            {
                var queryString = QueryStringSerializer.SerializeToString(request);
                if (!string.IsNullOrEmpty(queryString))
                {
                    absoluteUrl += "?" + queryString;
                }
            }

            try
            {
                absoluteUrl = new Uri(absoluteUrl).ToString();
            }
            catch (Exception ex)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Could not parse URL: " + absoluteUrl, ex);
            }

            var response = ResultsFilter?.Invoke(typeof(TResponse), httpMethod, absoluteUrl, request);
            if (response is TResponse)
            {
                var tcs = new TaskCompletionSource<TResponse>();
                tcs.SetResult((TResponse)response);
                return tcs.Task;
            }

            var httpReq = CreateRequest(httpMethod, absoluteUrl, request);
            var sendAsyncTask = client.SendAsync(httpReq, token);

            if (typeof(TResponse) == typeof(HttpResponseMessage))
            {
                return (Task<TResponse>)(object)sendAsyncTask;
            }

            return sendAsyncTask
                .ContinueWith(responseTask =>
                {
                    var httpRes = responseTask.Result;

                    if (httpRes.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        if (RefreshToken != null)
                        {
                            var refreshDto = new GetAccessToken { RefreshToken = RefreshToken, UseTokenCookie = UseTokenCookie };
                            var uri = this.RefreshTokenUri ?? this.BaseUri.CombineWith(refreshDto.ToPostUrl());

                            if (this.UseTokenCookie)
                            {
                                this.BearerToken = null;
                            }

                            return this.PostAsync<GetAccessTokenResponse>(uri, refreshDto, token)
                                .ContinueWith(t =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        if (t.Exception.UnwrapIfSingleException() is WebServiceException refreshEx)
                                        {
                                            throw new RefreshTokenException(refreshEx);
                                        }
                                        throw t.Exception;
                                    }

                                    var accessToken = t.Result?.AccessToken;
                                    var tokenCookie = this.GetTokenCookie();
                                    var refreshRequest = CreateRequest(httpMethod, absoluteUrl, request);

                                    if (UseTokenCookie)
                                    {
                                        if (tokenCookie == null)
                                            throw new RefreshTokenException("Could not retrieve new AccessToken Cooke from: " + uri);

                                        this.SetTokenCookie(tokenCookie);
                                    }
                                    else
                                    {
                                        if (string.IsNullOrEmpty(accessToken))
                                            throw new RefreshTokenException("Could not retrieve new AccessToken from: " + uri);

                                        if (tokenCookie != null)
                                        {
                                            this.SetTokenCookie(accessToken);
                                        }
                                        else
                                        {
                                            refreshRequest.AddBearerToken(this.BearerToken = accessToken);
                                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                                        }
                                    }

                                    return client.SendAsync(refreshRequest, token).ContinueWith(refreshTask =>
                                            ConvertToResponse<TResponse>(refreshTask.Result,
                                                httpMethod, absoluteUrl, refreshRequest, token),
                                        token).Unwrap();

                                }, token).Unwrap();
                        }
                        if (UserName != null && Password != null && client.DefaultRequestHeaders.Authorization == null)
                        {
                            AddBasicAuth(client);
                            httpReq = CreateRequest(httpMethod, absoluteUrl, request);
                            sendAsyncTask = client.SendAsync(httpReq, token);
                            return sendAsyncTask.ContinueWith(t =>
                                    ConvertToResponse<TResponse>(t.Result, httpMethod, absoluteUrl, request, token),
                                token).Unwrap();
                        }
                    }

                    return ConvertToResponse<TResponse>(httpRes, httpMethod, absoluteUrl, request, token);
                }, token).Unwrap();
        }

        private HttpRequestMessage CreateRequest(string httpMethod, string absoluteUrl, object request)
        {
            this.PopulateRequestMetadata(request);

            var httpReq = new HttpRequestMessage(new HttpMethod(httpMethod), absoluteUrl);

            foreach (var name in Headers.AllKeys)
            {
                httpReq.Headers.Add(name, Headers[name]);
            }
            httpReq.Headers.Add(HttpHeaders.Accept, ContentType);

            if (HttpUtils.HasRequestBody(httpMethod) && request != null)
            {
                if (request is HttpContent httpContent)
                {
                    httpReq.Content = httpContent;
                }
                else
                {
                    var str = request as string;
                    var bytes = request as byte[];
                    var stream = request as Stream;
                    if (str != null)
                    {
                        httpReq.Content = new StringContent(str);
                    }
                    else if (bytes != null)
                    {
                        httpReq.Content = new ByteArrayContent(bytes);
                    }
                    else if (stream != null)
                    {
                        httpReq.Content = new StreamContent(stream);
                    }
                    else
                    {
                        httpReq.Content = new StringContent(request.ToJson(), Encoding.UTF8, ContentType);
                    }

                    if (RequestCompressionType == CompressionTypes.Deflate)
                    {
                        httpReq.Content = new DeflateContent(httpReq.Content);
                    }
                    else if (RequestCompressionType == CompressionTypes.GZip)
                    {
                        httpReq.Content = new GzipContent(httpReq.Content);
                    }
                }
            }

            ApplyWebRequestFilters(httpReq);

            Interlocked.Increment(ref activeAsyncRequests);

            return httpReq;
        }

        private Task<TResponse> ConvertToResponse<TResponse>(HttpResponseMessage httpRes, string httpMethod, string absoluteUrl, object request, CancellationToken token)
        {
            ApplyWebResponseFilters(httpRes);

            if (!httpRes.IsSuccessStatusCode && ExceptionFilter != null)
            {
                var cachedResponse = ExceptionFilter(httpRes, absoluteUrl, typeof(TResponse));
                if (cachedResponse is TResponse)
                    return Task.FromResult((TResponse) cachedResponse);
            }

            if (typeof(TResponse) == typeof(string))
            {
                return httpRes.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                        var response = (TResponse) (object) task.Result;

                        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);
            }
            if (typeof(TResponse) == typeof(byte[]))
            {
                return httpRes.Content.ReadAsByteArrayAsync()
                    .ContinueWith(task =>
                    {
                        ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                        var response = (TResponse) (object) task.Result;

                        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);
            }
            if (typeof(TResponse) == typeof(Stream))
            {
                return httpRes.Content.ReadAsStreamAsync()
                    .ContinueWith(task =>
                    {
                        ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                        var response = (TResponse) (object) task.Result;

                        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);
            }

            return httpRes.Content.ReadAsStringAsync()
                .ContinueWith(task =>
                {
                    ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                    var body = task.Result;
                    var response = body.FromJson<TResponse>();

                    ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);

                    return response;
                }, token);
        }

        public Action<HttpRequestMessage> RequestFilter { get; set; }
        public static Action<HttpRequestMessage> GlobalRequestFilter { get; set; }

        private void ApplyWebRequestFilters(HttpRequestMessage httpReq)
        {
            RequestFilter?.Invoke(httpReq);
            GlobalRequestFilter?.Invoke(httpReq);
        }

        public Action<HttpResponseMessage> ResponseFilter { get; set; }
        public static Action<HttpResponseMessage> GlobalResponseFilter { get; set; }

        private void ApplyWebResponseFilters(HttpResponseMessage httpRes)
        {
            ResponseFilter?.Invoke(httpRes);
            GlobalResponseFilter?.Invoke(httpRes);
        }


        private void ThrowIfError<TResponse>(Task task, HttpResponseMessage httpRes, object request, string requestUri, object response)
        {
            Interlocked.Decrement(ref activeAsyncRequests);

            if (task.IsFaulted)
                throw CreateException<TResponse>(httpRes, task.Exception);

            if (!httpRes.IsSuccessStatusCode)
                ThrowResponseTypeException<TResponse>(httpRes, request, requestUri, response);
        }

        private void AddBasicAuth(HttpClient client)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
                return;

            var byteArray = Encoding.UTF8.GetBytes($"{UserName}:{Password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        protected T ResultFilter<T>(T response, HttpResponseMessage httpRes, string httpMethod, string requestUri, object request)
        {
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, requestUri, request);
            return response;
        }

        private static WebServiceException CreateException<TResponse>(HttpResponseMessage httpRes, Exception ex)
        {
            return new WebServiceException();
        }

        readonly ConcurrentDictionary<Type, Action<HttpResponseMessage, object, string, object>> ResponseHandlers
            = new ConcurrentDictionary<Type, Action<HttpResponseMessage, object, string, object>>();

        private void ThrowResponseTypeException<TResponse>(HttpResponseMessage httpRes, object request, string requestUri, object response)
        {
            var responseType = WebRequestUtils.GetErrorResponseDtoType<TResponse>(request);
            if (!ResponseHandlers.TryGetValue(responseType, out var responseHandler))
            {
                var mi = GetType().GetInstanceMethod("ThrowWebServiceException")
                    .MakeGenericMethod(new[] { responseType });

                responseHandler = (Action<HttpResponseMessage, object, string, object>)mi.CreateDelegate(
                    typeof(Action<HttpResponseMessage, object, string, object>), this);

                ResponseHandlers[responseType] = responseHandler;
            }
            responseHandler(httpRes, request, requestUri, response);
        }

        public static byte[] GetResponseBytes(object response)
        {
            if (response is Stream stream)
                return stream.ReadFully();

            if (response is byte[] bytes)
                return bytes;

            var str = response as string;
            return str?.ToUtf8Bytes();
        }

        public static WebServiceException ToWebServiceException(
            HttpResponseMessage httpRes, object response, Func<Stream, object> parseDtoFn)
        {
            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Status Code : {0}", httpRes.StatusCode);
                log.DebugFormat("Status Description : {0}", httpRes.ReasonPhrase);
            }

            var serviceEx = new WebServiceException(httpRes.ReasonPhrase)
            {
                StatusCode = (int)httpRes.StatusCode,
                StatusDescription = httpRes.ReasonPhrase,
                ResponseHeaders = httpRes.Headers.ToWebHeaderCollection()
            };

            try
            {
                var contentType = httpRes.GetContentType();

                var bytes = GetResponseBytes(response);
                if (bytes != null)
                {
                    if (string.IsNullOrEmpty(contentType) || contentType.MatchesContentType(MimeTypes.Json))
                    {
                        var stream = MemoryStreamFactory.GetStream(bytes);
                        serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                        serviceEx.ResponseDto = parseDtoFn?.Invoke(stream);

                        if (stream.CanRead)
                            stream.Dispose(); //alt ms throws when you dispose twice
                    }
                    else
                    {
                        serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                    }
                }
            }
            catch (Exception innerEx)
            {
                // Oh, well, we tried
                return new WebServiceException(httpRes.ReasonPhrase, innerEx)
                {
                    StatusCode = (int)httpRes.StatusCode,
                    StatusDescription = httpRes.ReasonPhrase,
                    ResponseBody = serviceEx.ResponseBody
                };
            }

            //Escape deserialize exception handling and throw here
            return serviceEx;
        }

        public void ThrowWebServiceException<TResponse>(HttpResponseMessage httpRes, object request, string requestUri, object response)
        {
            var webEx = ToWebServiceException(httpRes, response,
                stream => JsonSerializer.DeserializeFromStream<TResponse>(stream));

            throw webEx;

            //var authEx = ex as AuthenticationException;
            //if (authEx != null)
            //{
            //    throw WebRequestUtils.CreateCustomException(requestUri, authEx);
            //}
        }

        public virtual TResponse Send<TResponse>(object request)
        {
            return SendAsync<TResponse>(request).GetSyncResponse();
        }

        public virtual List<TResponse> SendAll<TResponse>(IEnumerable<object> requests)
        {
            return SendAllAsync<TResponse>(requests, default).GetSyncResponse();
        }

        public TResponse Send<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(httpMethod, relativeOrAbsoluteUrl, request).GetSyncResponse();
        }

        public virtual void Publish(object request)
        {
            PublishAsync(request, default).Wait();
        }

        public void PublishAll(IEnumerable<object> requestDtos)
        {
            PublishAllAsync(requestDtos, default).Wait();
        }

        public virtual Task<TResponse> SendAsync<TResponse>(object request) => SendAsync<TResponse>(request, default);
        public virtual Task<TResponse> SendAsync<TResponse>(object request, CancellationToken token)
        {
            if (typeof(TResponse) == typeof(object))
                return this.SendAsync(this.GetResponseType(request), request, token)
                    .ContinueWith(t => (TResponse)t.Result, token);

            if (request is IVerb)
            {
                if (request is IGet)
                    return GetAsync<TResponse>(request, token);
                if (request is IPost)
                    return PostAsync<TResponse>(request, token);
                if (request is IPut)
                    return PutAsync<TResponse>(request, token);
                if (request is IDelete)
                    return DeleteAsync<TResponse>(request, token);
                if (request is IPatch)
                    return PatchAsync<TResponse>(request, token);
            }

            if (request is IQuery)
                return GetAsync<TResponse>(request, token);
            if (request is ICrud)
            {
                var crudMethod = ServiceClientBase.ToHttpMethod(request.GetType());
                if (crudMethod != null)
                {
                    return crudMethod switch {
                        HttpMethods.Post => PostAsync<TResponse>(request, token),
                        HttpMethods.Put => PutAsync<TResponse>(request, token),
                        HttpMethods.Delete => DeleteAsync<TResponse>(request, token),
                        HttpMethods.Patch => PatchAsync<TResponse>(request, token),
                        HttpMethods.Get => GetAsync<TResponse>(request, token),
                        _ => throw new NotSupportedException("Unknown " + crudMethod),
                    };
                }
            }

            var httpMethod = ServiceClientBase.GetExplicitMethod(request) ?? DefaultHttpMethod;
            var requestUri = ResolveUrl(httpMethod, UrlResolver == null
                ? this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name
                : Format + "/reply/" + request.GetType().Name);

            return SendAsync<TResponse>(httpMethod, requestUri, request, token);
        }

        public virtual Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requests, CancellationToken token)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";
            this.PopulateRequestMetadatas(requests);
            return SendAsync<List<TResponse>>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests, token);
        }

        public virtual Task PublishAsync(object request, CancellationToken token)
        {
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
            return SendAsync<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), request, token);
        }

        public Task PublishAllAsync(IEnumerable<object> requests, CancellationToken token)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            this.PopulateRequestMetadatas(requests);
            return SendAsync<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests, token);
        }


        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto) =>
            SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, token);

        public Task<TResponse> GetAsync<TResponse>(object requestDto) =>
            SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        public Task<TResponse> GetAsync<TResponse>(object requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, token);

        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl) =>
            SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);
        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null, token);

        public Task GetAsync(IReturnVoid requestDto) =>
            SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        public Task GetAsync(IReturnVoid requestDto, CancellationToken token) =>
            SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, token);


        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto) =>
            SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, token);

        public Task<TResponse> DeleteAsync<TResponse>(object requestDto) =>
            SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        public Task<TResponse> DeleteAsync<TResponse>(object requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, token);

        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl) =>
            SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);
        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null, token);

        public Task DeleteAsync(IReturnVoid requestDto) =>
            SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        public Task DeleteAsync(IReturnVoid requestDto, CancellationToken token) =>
            SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, token);


        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto) =>
            SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto, token);

        public Task<TResponse> PostAsync<TResponse>(object requestDto) =>
            SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        public Task<TResponse> PostAsync<TResponse>(object requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto, token);

        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request) =>
            SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request);
        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request, token);

        public Task PostAsync(IReturnVoid requestDto) =>
            SendAsync<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        public Task PostAsync(IReturnVoid requestDto, CancellationToken token) =>
            SendAsync<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto, token);


        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto) =>
            SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);

        public Task<TResponse> PutAsync<TResponse>(object requestDto) =>
            SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        public Task<TResponse> PutAsync<TResponse>(object requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);

        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request) =>
            PutAsync<TResponse>(relativeOrAbsoluteUrl, request, default);
        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request, token);

        public Task PutAsync(IReturnVoid requestDto) => PutAsync(requestDto, default);
        public Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto) =>
            SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);

        public Task PutAsync(IReturnVoid requestDto, CancellationToken token) =>
            SendAsync<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);

        public Task<TResponse> PatchAsync<TResponse>(object requestDto) => PatchAsync<TResponse>(requestDto, default);
        public Task PatchAsync(IReturnVoid requestDto) => PatchAsync(requestDto, default);

        public Task<TResponse> PatchAsync<TResponse>(object requestDto, CancellationToken token) =>
            SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);

        public Task PatchAsync(IReturnVoid requestDto, CancellationToken token) =>
            SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto) =>
            CustomMethodAsync<TResponse>(httpVerb, requestDto, default);
        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, CancellationToken token)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto) =>
            CustomMethodAsync<TResponse>(httpVerb, requestDto, default);
        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto, CancellationToken token)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
        }

        public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto) => CustomMethodAsync(httpVerb, requestDto, default);
        public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto, CancellationToken token)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request) =>
            CustomMethodAsync<TResponse>(httpVerb, relativeOrAbsoluteUrl, request, default);
        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request, CancellationToken token)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? request : null;
            return SendAsync<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody, token);
        }


        public void SendOneWay(object request) => SendOneWay(request, default);
        public void SendOneWay(object request, CancellationToken token)
        {
            PublishAsync(request, token).Wait(token);
        }

        public void SendOneWay(string relativeOrAbsoluteUrl, object request) => SendOneWay(relativeOrAbsoluteUrl, request, default);
        public void SendOneWay(string relativeOrAbsoluteUrl, object request, CancellationToken token)
        {
            var httpMethod = ServiceClientBase.GetExplicitMethod(request) ?? DefaultHttpMethod;
            var absoluteUri = ToAbsoluteUrl(ResolveUrl(httpMethod, relativeOrAbsoluteUrl));
            SendAsync<byte[]>(httpMethod, absoluteUri, request, token).Wait(token);
        }

        public void SendAllOneWay(IEnumerable<object> requests) => SendAllOneWay(requests, default);
        public void SendAllOneWay(IEnumerable<object> requests, CancellationToken token)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            var absoluteUri = ToAbsoluteUrl(ResolveUrl(HttpMethods.Post, requestUri));
            this.PopulateRequestMetadatas(requests);
            SendAsync<byte[]>(HttpMethods.Post, absoluteUri, requests, token).Wait(token);
        }


        public void ClearCookies()
        {
            CookieContainer = new CookieContainer();
            HttpClient = null;
            HttpClient = GetHttpClient();
        }

        public Dictionary<string, string> GetCookieValues()
        {
            return CookieContainer.ToDictionary(BaseUri);
        }

        public void SetCookie(string name, string value, TimeSpan? expiresIn = null)
        {
            this.SetCookie(GetHttpClient().BaseAddress, name, value,
                expiresIn != null ? DateTime.UtcNow.Add(expiresIn.Value) : (DateTime?)null);
        }

        public void Get(IReturnVoid request)
        {
            GetAsync(request).WaitSyncResponse();
        }

        public TResponse Get<TResponse>(IReturn<TResponse> request)
        {
            return GetAsync(request).GetSyncResponse();
        }

        public TResponse Get<TResponse>(object request)
        {
            return GetAsync<TResponse>(request).GetSyncResponse();
        }

        public TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
        {
            return GetAsync<TResponse>(relativeOrAbsoluteUrl).GetSyncResponse();
        }

        public IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto) => throw new NotImplementedException();

        public void Delete(IReturnVoid requestDto)
        {
            DeleteAsync(requestDto).WaitSyncResponse();
        }

        public TResponse Delete<TResponse>(IReturn<TResponse> request)
        {
            return DeleteAsync(request).GetSyncResponse();
        }

        public TResponse Delete<TResponse>(object request)
        {
            return DeleteAsync<TResponse>(request).GetSyncResponse();
        }

        public TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
        {
            return DeleteAsync<TResponse>(relativeOrAbsoluteUrl).GetSyncResponse();
        }

        public void Post(IReturnVoid requestDto)
        {
            PostAsync(requestDto).WaitSyncResponse();
        }

        public TResponse Post<TResponse>(IReturn<TResponse> request)
        {
            return PostAsync(request).GetSyncResponse();
        }

        public TResponse Post<TResponse>(object request)
        {
            return PostAsync<TResponse>(request).GetSyncResponse();
        }

        public TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return PostAsync<TResponse>(relativeOrAbsoluteUrl, request).GetSyncResponse();
        }

        public void Put(IReturnVoid requestDto)
        {
            PutAsync(requestDto).WaitSyncResponse();
        }

        public TResponse Put<TResponse>(IReturn<TResponse> request)
        {
            return PutAsync(request).GetSyncResponse();
        }

        public TResponse Put<TResponse>(object request)
        {
            return PutAsync<TResponse>(request).GetSyncResponse();
        }

        public TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return PutAsync<TResponse>(relativeOrAbsoluteUrl, request).GetSyncResponse();
        }

        public void Patch(IReturnVoid request)
        {
            SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, request), request).WaitSyncResponse();
        }

        public TResponse Patch<TResponse>(IReturn<TResponse> request)
        {
            return SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, request), request).GetSyncResponse();
        }

        public TResponse Patch<TResponse>(object request)
        {
            return SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, request), request).GetSyncResponse();
        }

        public TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethods.Patch, relativeOrAbsoluteUrl, request).GetSyncResponse();
        }

        public void CustomMethod(string httpVerb, IReturnVoid request)
        {
            SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, request), request).WaitSyncResponse();
        }

        public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> request)
        {
            return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, request), request).GetSyncResponse();
        }

        public TResponse CustomMethod<TResponse>(string httpVerb, object request)
        {
            return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, request), null).GetSyncResponse();
        }

        public virtual Task<TResponse> PostFileAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType = null, CancellationToken token = default)
        {
            var content = new MultipartFormDataContent();
            var fileBytes = fileToUpload.ReadFully();
            var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = fileName
            };
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
            content.Add(fileContent, "file", fileName);

            return SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), content, token)
                .ContinueWith(t => { content.Dispose(); fileContent.Dispose(); return t.Result; },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
        {
            return PostFileAsync<TResponse>(relativeOrAbsoluteUrl, fileToUpload, fileName, mimeType).GetSyncResponse();
        }

        public Task<TResponse> PostFileWithRequestAsync<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload", CancellationToken token = default)
        {
            return PostFileWithRequestAsync<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), fileToUpload, fileName, request, fieldName, token);
        }

        public TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            return PostFileWithRequestAsync<TResponse>(fileToUpload, fileName, request, fileName).GetSyncResponse();
        }

        public virtual Task<TResponse> PostFileWithRequestAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
                                                        object request, string fieldName = "upload", CancellationToken token = default)
        {
            var queryString = QueryStringSerializer.SerializeToString(request);
            var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);

            var content = new MultipartFormDataContent();

            foreach (string key in nameValueCollection)
            {
                var value = nameValueCollection[key];
                content.Add(new StringContent(value), $"\"{key}\"");
            }

            var fileBytes = fileToUpload.ReadFully();
            var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = fileName
            };
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MimeTypes.GetMimeType(fileName));
            content.Add(fileContent, "file", fileName);

            return SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), content, token)
                .ContinueWith(t => { content.Dispose(); fileContent.Dispose(); return t.Result; },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
                                                        object request, string fieldName = "upload")
        {
            return PostFileWithRequestAsync<TResponse>(relativeOrAbsoluteUrl, fileToUpload, fileName, request, fileName).GetSyncResponse();
        }

        public TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequestAsync<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), request, files.ToArray()).GetSyncResponse();
        }

        public TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequestAsync<TResponse>(ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request, files.ToArray()).GetSyncResponse();
        }

        public Task<TResponse> PostFilesWithRequestAsync<TResponse>(object request, IEnumerable<UploadFile> files, CancellationToken token = default)
        {
            return PostFilesWithRequestAsync<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), request, files.ToArray(), token);
        }

        public Task<TResponse> PostFilesWithRequestAsync<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files, CancellationToken token = default)
        {
            return PostFilesWithRequestAsync<TResponse>(ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request, files.ToArray(), token);
        }

        public virtual Task<TResponse> PostFilesWithRequestAsync<TResponse>(string requestUri, object request, UploadFile[] files, CancellationToken token = default)
        {
            var queryString = QueryStringSerializer.SerializeToString(request);
            var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);

            var content = new MultipartFormDataContent();

            foreach (string key in nameValueCollection)
            {
                var value = nameValueCollection[key];
                content.Add(new StringContent(value), $"\"{key}\"");
            }

            var disposables = new List<IDisposable> { content };

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileBytes = file.Stream.ReadFully();
                var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
                disposables.Add(fileContent);
                var fieldName = file.FieldName ?? $"upload{i}";
                var fileName = file.FileName ?? $"upload{i}";
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = fieldName,
                    FileName = fileName,
                };

                var contentType = file.ContentType ?? (file.FileName != null ? MimeTypes.GetMimeType(file.FileName) : null) ?? "application/octet-stream";
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                content.Add(fileContent, fileName, fileName);
            }

            return SendAsync<TResponse>(HttpMethods.Post, requestUri, content, token)
                .ContinueWith(t => { foreach (var d in disposables) d.Dispose(); return t.Result; },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public void CancelAsync() => throw new NotSupportedException("Pass CancellationToken when calling each async API");

        public void Dispose()
        {
            HttpClient?.Dispose();
            HttpClient = null;
        }
    }

    public delegate object ResultsFilterHttpDelegate(Type responseType, string httpMethod, string requestUri, object request);

    public delegate void ResultsFilterHttpResponseDelegate(HttpResponseMessage webResponse, object response, string httpMethod, string requestUri, object request);

    public delegate object ExceptionFilterHttpDelegate(HttpResponseMessage webResponse, string requestUri, Type responseType);

    public static class JsonHttpClientUtils
    {
        public static Dictionary<string, string> ToDictionary(this HttpResponseHeaders headers)
        {
            var to = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                to[header.Key] = string.Join(", ", header.Value);
            }
            return to;
        }

        public static WebHeaderCollection ToWebHeaderCollection(this HttpResponseHeaders headers)
        {
            var to = new WebHeaderCollection();
            foreach (var header in headers)
            {
                to[header.Key] = string.Join(", ", header.Value);
            }
            return to;
        }

        public static string GetContentType(this HttpResponseMessage httpRes)
        {
            return httpRes.Headers.TryGetValues(HttpHeaders.ContentType, out var values)
                ? values.FirstOrDefault()
                : null;
        }

        public static void AddBearerToken(this HttpRequestMessage client, string bearerToken)
        {
            if (string.IsNullOrEmpty(bearerToken))
                return;

            client.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    internal static class InternalExtensions
    {
        public static T GetSyncResponse<T>(this Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (Exception ex)
            {
                throw ex.UnwrapIfSingleException();
            }
        }

        public static void WaitSyncResponse(this Task task)
        {
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                throw ex.UnwrapIfSingleException();
            }
        }
    }
}
