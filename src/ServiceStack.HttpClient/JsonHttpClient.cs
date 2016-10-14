// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

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
        public static string DefaultUserAgent = "ServiceStack .NET HttpClient " + Env.ServiceStackVersion;

        public string BaseUri { get; set; }

        public string Format { get; private set; }
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

        public CancellationTokenSource CancelTokenSource { get; set; }

        /// <summary>
        /// Gets the collection of headers to be added to outgoing requests.
        /// </summary>
        public INameValueCollection Headers { get; private set; }

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
            this.Headers = PclExportClient.Instance.NewNameValueCollection();
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
            return ToAbsoluteUrl((UrlResolver != null
                ? UrlResolver(this, httpMethod, relativeOrAbsoluteUrl)
                : null) ?? relativeOrAbsoluteUrl);
        }

        public virtual string ResolveTypedUrl(string httpMethod, object requestDto)
        {
            return ToAbsoluteUrl((TypedUrlResolver != null
                ? TypedUrlResolver(this, httpMethod, requestDto)
                : null) ?? requestDto.ToUrl(httpMethod, Format));
        }

        [Obsolete("Renamed to ToAbsoluteUrl")]
        public virtual string GetBaseUrl(string relativeOrAbsoluteUrl)
        {
            return ToAbsoluteUrl(relativeOrAbsoluteUrl);
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

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default(CancellationToken))
        {
            if (!httpMethod.HasRequestBody() && request != null)
            {
                var queryString = QueryStringSerializer.SerializeToString(request);
                if (!string.IsNullOrEmpty(queryString))
                {
                    absoluteUrl += "?" + queryString;
                }
            }

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, absoluteUrl, request);
                if (response is TResponse)
                {
                    var tcs = new TaskCompletionSource<TResponse>();
                    tcs.SetResult((TResponse)response);
                    return tcs.Task;
                }
            }

            var client = GetHttpClient();

            this.PopulateRequestMetadata(request);

            var httpReq = new HttpRequestMessage(new HttpMethod(httpMethod), absoluteUrl);

            foreach (var name in Headers.AllKeys)
            {
                httpReq.Headers.Add(name, Headers[name]);
            }
            httpReq.Headers.Add(HttpHeaders.Accept, ContentType);

            if (httpMethod.HasRequestBody() && request != null)
            {
                var httpContent = request as HttpContent;
                if (httpContent != null)
                {
                    httpReq.Content = httpContent;
                }
                else
                {
                    var str = request as string;
                    var bytes = request as byte[];
                    var stream = request as Stream;
                    if (str != null)
                        httpReq.Content = new StringContent(str);
                    else if (bytes != null)
                        httpReq.Content = new ByteArrayContent(bytes);
                    else if (stream != null)
                        httpReq.Content = new StreamContent(stream);
                    else
                    {
                        httpReq.Content = new StringContent(request.ToJson(), Encoding.UTF8, ContentType);
                    }
                }
            }

            ApplyWebRequestFilters(httpReq);

            Interlocked.Increment(ref activeAsyncRequests);

            if (token == default(CancellationToken))
            {
                if (CancelTokenSource == null)
                    CancelTokenSource = new CancellationTokenSource();
                token = CancelTokenSource.Token;
            }

            var sendAsyncTask = client.SendAsync(httpReq, token);

            if (typeof(TResponse) == typeof(HttpResponseMessage))
            {
                return (Task<TResponse>)(object)sendAsyncTask;
            }

            return sendAsyncTask
                .ContinueWith(responseTask =>
                {
                    var httpRes = responseTask.Result;
                    ApplyWebResponseFilters(httpRes);

                    if (!httpRes.IsSuccessStatusCode && ExceptionFilter != null)
                    {
                        var cachedResponse = ExceptionFilter(httpRes, absoluteUrl, typeof(TResponse));
                        if (cachedResponse is TResponse)
                            return Task.FromResult((TResponse)cachedResponse);
                    }

                    if (typeof(TResponse) == typeof(string))
                    {
                        return httpRes.Content.ReadAsStringAsync().ContinueWith(task =>
                        {
                            ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                            var response = (TResponse)(object)task.Result;

                            if (ResultsFilterResponse != null)
                                ResultsFilterResponse(httpRes, response, httpMethod, absoluteUrl, request);

                            return response;
                        }, token);
                    }
                    if (typeof(TResponse) == typeof(byte[]))
                    {
                        return httpRes.Content.ReadAsByteArrayAsync().ContinueWith(task =>
                        {
                            ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                            var response = (TResponse)(object)task.Result;

                            if (ResultsFilterResponse != null)
                                ResultsFilterResponse(httpRes, response, httpMethod, absoluteUrl, request);

                            return response;
                        }, token);
                    }
                    if (typeof(TResponse) == typeof(Stream))
                    {
                        return httpRes.Content.ReadAsStreamAsync().ContinueWith(task =>
                        {
                            ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                            var response = (TResponse)(object)task.Result;

                            if (ResultsFilterResponse != null)
                                ResultsFilterResponse(httpRes, response, httpMethod, absoluteUrl, request);

                            return response;
                        }, token);
                    }

                    return httpRes.Content.ReadAsStringAsync().ContinueWith(task =>
                    {
                        ThrowIfError<TResponse>(task, httpRes, request, absoluteUrl, task.Result);

                        var body = task.Result;
                        var response = body.FromJson<TResponse>();

                        if (ResultsFilterResponse != null)
                            ResultsFilterResponse(httpRes, response, httpMethod, absoluteUrl, request);

                        return response;
                    }, token);
                }, token).Unwrap();
        }

        private void DisposeCancelToken()
        {
            if (Interlocked.Decrement(ref activeAsyncRequests) > 0) return;

            if (CancelTokenSource == null) return;
            
            CancelTokenSource.Dispose();
            CancelTokenSource = null;
        }

        public Action<HttpRequestMessage> RequestFilter { get; set; }
        public static Action<HttpRequestMessage> GlobalRequestFilter { get; set; }

        private void ApplyWebRequestFilters(HttpRequestMessage httpReq)
        {
            if (RequestFilter != null)
                RequestFilter(httpReq);

            if (GlobalRequestFilter != null)
                GlobalRequestFilter(httpReq);
        }

        public Action<HttpResponseMessage> ResponseFilter { get; set; }
        public static Action<HttpResponseMessage> GlobalResponseFilter { get; set; }

        private void ApplyWebResponseFilters(HttpResponseMessage httpRes)
        {
            if (ResponseFilter != null)
                ResponseFilter(httpRes);

            if (GlobalResponseFilter != null)
                GlobalResponseFilter(httpRes);
        }


        private void ThrowIfError<TResponse>(Task task, HttpResponseMessage httpRes, object request, string requestUri, object response)
        {
            DisposeCancelToken();

            if (task.IsFaulted)
                throw CreateException<TResponse>(httpRes, task.Exception);

            if (!httpRes.IsSuccessStatusCode)
                ThrowResponseTypeException<TResponse>(httpRes, request, requestUri, response);
        }

        private void AddBasicAuth(HttpClient client)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
                return;

            var byteArray = Encoding.UTF8.GetBytes("{0}:{1}".Fmt(UserName, Password));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        protected T ResultFilter<T>(T response, HttpResponseMessage httpRes, string httpMethod, string requestUri, object request)
        {
            if (ResultsFilterResponse != null)
            {
                ResultsFilterResponse(httpRes, response, httpMethod, requestUri, request);
            }
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
            Action<HttpResponseMessage, object, string, object> responseHandler;
            if (!ResponseHandlers.TryGetValue(responseType, out responseHandler))
            {
                var mi = GetType().GetInstanceMethod("ThrowWebServiceException")
                    .MakeGenericMethod(new[] { responseType });

                responseHandler = (Action<HttpResponseMessage, object, string, object>)mi.CreateDelegate(
                    typeof(Action<HttpResponseMessage, object, string, object>), this);

                ResponseHandlers[responseType] = responseHandler;
            }
            responseHandler(httpRes, request, requestUri, response);
        }

        public byte[] GetResponseBytes(object response)
        {
            var stream = response as Stream;
            if (stream != null)
                return stream.ReadFully();
            
            var bytes = response as byte[];
            if (bytes != null)
                return bytes;

            var str = response as string;
            if (str != null)
                return str.ToUtf8Bytes();

            return null;
        }

        public void ThrowWebServiceException<TResponse>(HttpResponseMessage httpRes, object request, string requestUri, object response)
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
                    if (string.IsNullOrEmpty(contentType) || contentType.MatchesContentType(ContentType))
                    {
                        var stream = MemoryStreamFactory.GetStream(bytes);
                        serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                        serviceEx.ResponseDto = JsonSerializer.DeserializeFromStream<TResponse>(stream);

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
                throw new WebServiceException(httpRes.ReasonPhrase, innerEx)
                {
                    StatusCode = (int)httpRes.StatusCode,
                    StatusDescription = httpRes.ReasonPhrase,
                    ResponseBody = serviceEx.ResponseBody
                };
            }

            //Escape deserialize exception handling and throw here
            throw serviceEx;

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
            return SendAllAsync<TResponse>(requests, default(CancellationToken)).GetSyncResponse();
        }

        public virtual void Publish(object request)
        {
            PublishAsync(request, default(CancellationToken)).Wait();
        }

        public void PublishAll(IEnumerable<object> requestDtos)
        {
            PublishAllAsync(requestDtos, default(CancellationToken)).Wait();
        }

        public virtual Task<TResponse> SendAsync<TResponse>(object request)
        {
            return SendAsync<TResponse>(request, default(CancellationToken));
        }

        public virtual Task<TResponse> SendAsync<TResponse>(object request, CancellationToken token)
        {
            if (typeof(TResponse) == typeof(object))
                return this.SendAsync(this.GetResponseType(request), request, token)
                    .ContinueWith(t => (TResponse)t.Result, token);

            if (request is IVerb)
            {
                if (request is IGet)
                    return GetAsync<TResponse>(request);
                if (request is IPost)
                    return PostAsync<TResponse>(request);
                if (request is IPut)
                    return PutAsync<TResponse>(request);
                if (request is IDelete)
                    return DeleteAsync<TResponse>(request);
                if (request is IPatch)
                    return PatchAsync<TResponse>(request);
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
            return SendAsync<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests, token);
        }


        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        public Task<TResponse> GetAsync<TResponse>(object requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);
        }

        public Task GetAsync(IReturnVoid requestDto)
        {
            return SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }


        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        public Task<TResponse> DeleteAsync<TResponse>(object requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);
        }

        public Task DeleteAsync(IReturnVoid requestDto)
        {
            return SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }


        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public Task<TResponse> PostAsync<TResponse>(object requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request);
        }

        public Task PostAsync(IReturnVoid requestDto)
        {
            return SendAsync<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }



        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public Task<TResponse> PutAsync<TResponse>(object requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return SendAsync<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request);
        }

        public Task PutAsync(IReturnVoid requestDto)
        {
            return SendAsync<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }


        public Task<TResponse> PatchAsync<TResponse>(object requestDto)
        {
            return SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }


        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? request : null;
            return SendAsync<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody);
        }


        public void SendOneWay(object request)
        {
            PublishAsync(request, default(CancellationToken)).Wait();
        }

        public void SendOneWay(string relativeOrAbsoluteUrl, object request)
        {
            var httpMethod = ServiceClientBase.GetExplicitMethod(request) ?? DefaultHttpMethod;
            var absolutetUri = ToAbsoluteUrl(ResolveUrl(httpMethod, relativeOrAbsoluteUrl));
            SendAsync<byte[]>(httpMethod, absolutetUri, request).Wait();
        }

        public void SendAllOneWay(IEnumerable<object> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            var absolutetUri = ToAbsoluteUrl(ResolveUrl(HttpMethods.Post, requestUri));
            SendAsync<byte[]>(HttpMethods.Post, absolutetUri, requests).Wait();
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

        public IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto)
        {
            throw new NotImplementedException();
        }

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
            SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, request), null).WaitSyncResponse();
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

        public virtual Task<TResponse> PostFileAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType = null)
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

            return SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), content)
                .ContinueWith(t => { content.Dispose(); fileContent.Dispose(); return t.Result; },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
        {
            return PostFileAsync<TResponse>(relativeOrAbsoluteUrl, fileToUpload, fileName, mimeType).GetSyncResponse();
        }

        public Task<TResponse> PostFileWithRequestAsync<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            return PostFileWithRequestAsync<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), fileToUpload, fileName, request, fieldName);
        }

        public TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            return PostFileWithRequestAsync<TResponse>(fileToUpload, fileName, request, fileName).GetSyncResponse();
        }

        public virtual Task<TResponse> PostFileWithRequestAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
                                                        object request, string fieldName = "upload")
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

            return SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), content)
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

        public Task<TResponse> PostFilesWithRequestAsync<TResponse>(object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequestAsync<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), request, files.ToArray());
        }

        public Task<TResponse> PostFilesWithRequestAsync<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequestAsync<TResponse>(ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request, files.ToArray());
        }

        public virtual Task<TResponse> PostFilesWithRequestAsync<TResponse>(string requestUri, object request, UploadFile[] files)
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
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MimeTypes.GetMimeType(fileName));

                content.Add(fileContent, fileName, fileName);
            }

            return SendAsync<TResponse>(HttpMethods.Post, requestUri, content)
                .ContinueWith(t => { foreach (var d in disposables) d.Dispose(); return t.Result; },
                TaskContinuationOptions.ExecuteSynchronously);
        }


        public void CancelAsync()
        {
            CancelTokenSource.Cancel();
        }

        public void Dispose()
        {
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
            IEnumerable<string> values;
            return httpRes.Headers.TryGetValues(HttpHeaders.ContentType, out values) 
                ? values.FirstOrDefault() 
                : null;
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