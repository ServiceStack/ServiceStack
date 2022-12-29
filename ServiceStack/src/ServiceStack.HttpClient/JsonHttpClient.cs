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
using ServiceStack.Caching;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack;

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
    public bool EnableAutoRefreshToken { get; set; } = true;

    public bool UseCookies { get; set; } = true;

    /// <summary>
    /// Gets the collection of headers to be added to outgoing requests.
    /// </summary>
    public NameValueCollection Headers { get; private set; }

    public JsonHttpClient SetBaseUri(string baseUri)
    {
        this.BasePath = "/" + Format + "/reply/";
        this.BaseUri = baseUri;
        this.SyncReplyBaseUri = baseUri.WithTrailingSlash() + Format + "/reply/";
        this.AsyncOneWayBaseUri = baseUri.WithTrailingSlash() + Format + "/oneway/";
        return this;
    }

    public JsonHttpClient SetBaseUri(string baseUri, string basePath) => 
        SetBaseUri(baseUri).WithBasePath(basePath);

    /// <summary>
    /// Relative BasePath to use for predefined routes. Set with `UseBasePath` or `WithBasePath()`
    /// Always contains '/' prefix + '/' suffix, e.g. /api/
    /// </summary>
    public string BasePath { get; protected set; }

    /// <summary>
    /// Replace the Base reply/oneway paths to use a different prefix
    /// </summary>
    public string UseBasePath
    {
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                SetBaseUri(this.BaseUri);
            }
            else
            {
                this.BasePath = (value[0] != '/' ? '/' + value : value).WithTrailingSlash();
                this.SyncReplyBaseUri = this.BaseUri.CombineWith(BasePath);
                this.AsyncOneWayBaseUri = this.BaseUri.CombineWith(BasePath);
            }
        }
    }

    public JsonHttpClient WithBasePath(string basePath)
    {
        UseBasePath = basePath;
        return this;
    }

    public JsonHttpClient(HttpClient httpClient) : this()
    {
        this.HttpClient = httpClient;
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
        return ToAbsoluteUrl(TypedUrlResolver?.Invoke(this, httpMethod, requestDto) 
            ?? requestDto.ToUrl(httpMethod, fallback:requestType => BasePath + requestType.GetOperationName()));
    }

    public HttpClient GetHttpClient()
    {
        //Should reuse same instance: http://social.msdn.microsoft.com/Forums/en-US/netfxnetcom/thread/4e12d8e2-e0bf-4654-ac85-3d49b07b50af/
        if (HttpClient != null)
            return HttpClient;

        var handler = HttpMessageHandler;

        if (handler == null && GlobalHttpMessageHandlerFactory != null)
            handler = GlobalHttpMessageHandlerFactory();

        if (handler == null)
        {
            var useHandler = new HttpClientHandler
            {
                CookieContainer = CookieContainer,
                UseDefaultCredentials = Credentials == null,
                Credentials = Credentials,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            if (UseCookies) //UseCookies throws in Blazor, only set if true
                useHandler.UseCookies = UseCookies;
                
            handler = useHandler;
        }
            
        var baseUri = BaseUri != null ? new Uri(BaseUri) : null;

        var client = new HttpClient(handler, disposeHandler: HttpMessageHandler == null) { BaseAddress = baseUri };

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
    
    internal sealed class CompressContent : HttpContent
    {
        private readonly HttpContent content;
        private IStreamCompressor compressor;
        public CompressContent(HttpContent content, IStreamCompressor compressor)
        {
            this.content = content;
            this.compressor = compressor;
            
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Add(compressor.Encoding);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using var zip = compressor.Compress(stream, leaveOpen:true);
            await content.CopyToAsync(zip).ConfigAwait();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    public async Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default)
    {
        var client = GetHttpClient();
        if (!HttpUtils.HasRequestBody(httpMethod) && request != null)
        {
            var queryString = QueryStringSerializer.SerializeToString(request);
            if (!string.IsNullOrEmpty(queryString))
                absoluteUrl += "?" + queryString;
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

        var filterResponse = ResultsFilter?.Invoke(typeof(TResponse), httpMethod, absoluteUrl, request);
        if (filterResponse is TResponse typedResponse)
            return typedResponse;

        var httpReq = CreateRequest(httpMethod, absoluteUrl, request);

        try
        {
            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();

            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (TResponse)(object) httpRes;

            if (httpRes.StatusCode == HttpStatusCode.Unauthorized)
            {
                var hasRefreshToken = RefreshToken != null;
                if (EnableAutoRefreshToken && hasRefreshToken)
                {
                    var refreshDto = new GetAccessToken {
                        RefreshToken = RefreshToken,
                    };
                    var uri = this.RefreshTokenUri ?? this.BaseUri.CombineWith(refreshDto.ToPostUrl());

                    this.BearerToken = null;
                    this.CookieContainer?.DeleteCookie(new Uri(BaseUri), "ss-tok");

                    try
                    {
                        var accessTokenResponse = await this.PostAsync<GetAccessTokenResponse>(uri, refreshDto, token).ConfigAwait();
                            
                        var accessToken = accessTokenResponse?.AccessToken;
                        var tokenCookie = this.GetTokenCookie();
                        var refreshRequest = CreateRequest(httpMethod, absoluteUrl, request);

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            refreshRequest.AddBearerToken(this.BearerToken = accessToken);
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        }
                        else if (tokenCookie != null)
                        {
                            this.SetTokenCookie(tokenCookie);
                        }
                        else throw new RefreshTokenException("Could not retrieve new AccessToken from: " + uri);
                            
                        var refreshTokenResponse = await client.SendAsync(refreshRequest, token).ConfigAwait();
                        return await ConvertToResponse<TResponse>(refreshTokenResponse, httpMethod, absoluteUrl, refreshRequest, token).ConfigAwait();
                    }
                    catch (Exception e)
                    {
                        if (e.UnwrapIfSingleException() is WebServiceException refreshEx)
                            throw new RefreshTokenException(refreshEx);

                        throw;
                    }
                }

                if (UserName != null && Password != null && client.DefaultRequestHeaders.Authorization == null)
                {
                    AddBasicAuth(client);
                    httpReq = CreateRequest(httpMethod, absoluteUrl, request);
                    var response = await client.SendAsync(httpReq, token).ConfigAwait();
                    return await ConvertToResponse<TResponse>(response, httpMethod, absoluteUrl, request, token).ConfigAwait();
                }
            }

            return await ConvertToResponse<TResponse>(httpRes, httpMethod, absoluteUrl, request, token).ConfigAwait();
        }
        catch (Exception e)
        {
            log.Error(e, "HttpClient Exception: " + e.Message);
            throw;
        }
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
                if (request is string str)
                {
                    httpReq.Content = new StringContent(str);
                }
                else if (request is byte[] bytes)
                {
                    httpReq.Content = new ByteArrayContent(bytes);
                }
                else if (request is Stream stream)
                {
                    httpReq.Content = new StreamContent(stream);
                }
                else
                {
                    httpReq.Content = new StringContent(request.ToJson(), Encoding.UTF8, ContentType);
                }

                var compressor = StreamCompressors.Get(RequestCompressionType);
                if (compressor != null)
                    httpReq.Content = new CompressContent(httpReq.Content, compressor);
            }
        }

        ApplyWebRequestFilters(httpReq);

        Interlocked.Increment(ref activeAsyncRequests);

        return httpReq;
    }

    private async Task<TResponse> ConvertToResponse<TResponse>(HttpResponseMessage httpRes, string httpMethod, string absoluteUrl, object request, CancellationToken token)
    {
        ApplyWebResponseFilters(httpRes);

        if (!httpRes.IsSuccessStatusCode && ExceptionFilter != null)
        {
            var cachedResponse = ExceptionFilter(httpRes, absoluteUrl, typeof(TResponse));
            if (cachedResponse is TResponse filterResponse)
                return filterResponse;
        }

        if (typeof(TResponse) == typeof(string))
        {
            var result = await ThrowIfError(() => httpRes.Content.ReadAsStringAsync(), httpRes, request, absoluteUrl).ConfigAwait();
            var response = (TResponse) (object) result;
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
            return response;
        }
        if (typeof(TResponse) == typeof(byte[]))
        {
            var result = await ThrowIfError(() => httpRes.Content.ReadAsByteArrayAsync(), httpRes, request, absoluteUrl).ConfigAwait();
            var response = (TResponse) (object) result;
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
            return response;
        }
        if (typeof(TResponse) == typeof(Stream))
        {
            var result = await ThrowIfError(() => httpRes.Content.ReadAsStreamAsync(), httpRes, request, absoluteUrl).ConfigAwait();
            var response = (TResponse) (object) result;
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
            return response;
        }

        var json = await ThrowIfError(() => httpRes.Content.ReadAsStringAsync(), httpRes, request, absoluteUrl).ConfigAwait();
        var obj = json.FromJson<TResponse>();
        ResultsFilterResponse?.Invoke(httpRes, obj, httpMethod, absoluteUrl, request);
        return obj;
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

    private async Task<TResponse> ThrowIfError<TResponse>(Func<Task<TResponse>> fn, HttpResponseMessage httpRes, object request, string requestUri)
    {
        Interlocked.Decrement(ref activeAsyncRequests);

        TResponse response;
        try
        {
            response = await fn().ConfigAwait();
        }
        catch (Exception e)
        {
            throw CreateException<TResponse>(httpRes, e);
        }

        if (!httpRes.IsSuccessStatusCode)
            ThrowResponseTypeException<TResponse>(httpRes, request, requestUri, response);

        return response;
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

    readonly ConcurrentDictionary<Type, Action<HttpResponseMessage, object, string, object>> ResponseHandlers = new();

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
    public virtual async Task<TResponse> SendAsync<TResponse>(object request, CancellationToken token)
    {
        if (typeof(TResponse) == typeof(object))
        {
            var result = await this.SendAsync(this.GetResponseType(request), request, token).ConfigAwait();
            return (TResponse) result;
        }

        var httpMethod = ServiceClientUtils.GetHttpMethod(request.GetType());
        if (httpMethod != null)
        {
            return httpMethod switch {
                HttpMethods.Get => await GetAsync<TResponse>(request, token).ConfigAwait(),
                HttpMethods.Post => await PostAsync<TResponse>(request, token).ConfigAwait(),
                HttpMethods.Put => await PutAsync<TResponse>(request, token).ConfigAwait(),
                HttpMethods.Delete => await DeleteAsync<TResponse>(request, token).ConfigAwait(),
                HttpMethods.Patch => await PatchAsync<TResponse>(request, token).ConfigAwait(),
                _ => throw new NotSupportedException("Unknown " + httpMethod),
            };
        }

        httpMethod = DefaultHttpMethod;
        var requestUri = ResolveUrl(httpMethod, UrlResolver == null
            ? this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name
            : this.BasePath + request.GetType().Name);

        return await SendAsync<TResponse>(httpMethod, requestUri, request, token).ConfigAwait();
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
        SendAsync<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request);
    public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request, token);

    public Task PutAsync(IReturnVoid requestDto) =>
        SendAsync<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
    public Task PutAsync(IReturnVoid requestDto, CancellationToken token) =>
        SendAsync<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);

        

    public Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto) =>
        SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
    public Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);

    public Task<TResponse> PatchAsync<TResponse>(object requestDto) =>
        SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
    public Task<TResponse> PatchAsync<TResponse>(object requestDto, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);

    public Task<TResponse> PatchAsync<TResponse>(string relativeOrAbsoluteUrl, object request) =>
        SendAsync<TResponse>(HttpMethods.Patch, ResolveUrl(HttpMethods.Patch, relativeOrAbsoluteUrl), request);
    public Task<TResponse> PatchAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Patch, ResolveUrl(HttpMethods.Patch, relativeOrAbsoluteUrl), request, token);

    public Task PatchAsync(IReturnVoid requestDto) =>
        SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
    public Task PatchAsync(IReturnVoid requestDto, CancellationToken token) =>
        SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);
        
        
    public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, CancellationToken token = default)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
    }

    public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto, CancellationToken token = default)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
    }

    public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto, CancellationToken token = default)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
    }

    public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request, CancellationToken token = default)
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
        var httpMethod = ServiceClientUtils.GetHttpMethod(request.GetType()) ?? DefaultHttpMethod;
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
            expiresIn != null ? DateTime.UtcNow.Add(expiresIn.Value) : null);
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

    public string GetHttpMethod(object request) => ServiceClientUtils.GetHttpMethod(request.GetType());

    public virtual async Task<TResponse> PostFileAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType = null, string fieldName = "file", CancellationToken token = default)
    {
        using var content = new MultipartFormDataContent();
        var fileBytes = await fileToUpload.ReadFullyAsync(token).ConfigAwait();
        using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = fieldName,
            FileName = fileName
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
        content.Add(fileContent, fieldName, fileName);

        var result = await SendAsync<TResponse>(HttpMethods.Post,
            ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), content, token).ConfigAwait();
        return result;
    }

    public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType, string fieldName="file")
    {
        return PostFileAsync<TResponse>(relativeOrAbsoluteUrl, fileToUpload, fileName:fileName, mimeType:mimeType, fieldName:fieldName).GetSyncResponse();
    }

    public Task<TResponse> PostFileWithRequestAsync<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "file", CancellationToken token = default)
    {
        return PostFileWithRequestAsync<TResponse>(ResolveTypedUrl(GetHttpMethod(request) ?? HttpMethods.Post, request), fileToUpload, 
            fileName:fileName, request:request, fieldName:fieldName, token);
    }

    public TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "file")
    {
        return PostFileWithRequestAsync<TResponse>(fileToUpload, fileName:fileName, request:request, fieldName:fileName).GetSyncResponse();
    }

    public virtual async Task<TResponse> PostFileWithRequestAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
        object request, string fieldName = "file", CancellationToken token = default)
    {
        var queryString = QueryStringSerializer.SerializeToString(request);
        var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);

        using var content = new MultipartFormDataContent();

        foreach (string key in nameValueCollection)
        {
            var value = nameValueCollection[key];
            content.Add(new StringContent(value), $"\"{key}\"");
        }

        var fileBytes = await fileToUpload.ReadFullyAsync(token).ConfigAwait();
        using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = fieldName,
            FileName = fileName
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MimeTypes.GetMimeType(fileName));
        content.Add(fileContent, fieldName, fileName);

        var httpMethod = GetHttpMethod(request) ?? HttpMethods.Post;
        var result = await SendAsync<TResponse>(httpMethod, ResolveUrl(httpMethod, relativeOrAbsoluteUrl),
            content, token).ConfigAwait();
        return result;
    }

    public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
        object request, string fieldName = "file")
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

    public virtual async Task<TResponse> PostFilesWithRequestAsync<TResponse>(string requestUri, object request, UploadFile[] files, CancellationToken token = default)
    {
        var queryString = QueryStringSerializer.SerializeToString(request);
        var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);

        using var content = new MultipartFormDataContent();

        foreach (string key in nameValueCollection)
        {
            var value = nameValueCollection[key];
            content.Add(new StringContent(value), $"\"{key}\"");
        }

        var disposables = new List<IDisposable>();
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var fileBytes = await file.Stream.ReadFullyAsync(token).ConfigAwait();
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

        try
        {
            var result = await SendAsync<TResponse>(HttpMethods.Post, requestUri, content, token).ConfigAwait();
            return result;
        }
        finally
        {
            foreach (var d in disposables) d.Dispose();
        }
    }

    public void CancelAsync() => throw new NotSupportedException("Pass CancellationToken when calling each async API");

    public void Dispose()
    {
        HttpClient?.Dispose();
        HttpClient = null;
    }
}

internal static class InternalExtensions
{
    public static T GetSyncResponse<T>(this Task<T> task)
    {
        try
        {
            return task.GetAwaiter().GetResult();
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
