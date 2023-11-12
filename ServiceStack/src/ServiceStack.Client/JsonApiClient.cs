#nullable enable
#if NET6_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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

public interface IHasJsonApiClient
{
    public JsonApiClient? Client { get; }
}
public interface IServiceGatewayFormAsync
{
    Task<TResponse> SendFormAsync<TResponse>(object requestDto, MultipartFormDataContent formData, CancellationToken token = default);
}
public interface IClientFactory
{
    IServiceGateway GetGateway();
    JsonApiClient GetClient();
}
public interface ICloneServiceGateway
{
    IServiceGateway Clone();
}

/// <summary>
/// JsonApiClient designed to work with 
/// </summary>
public class JsonApiClient : IJsonServiceClient, IHasCookieContainer, IServiceClientMeta, IServiceGatewayFormAsync
{
    public static string DefaultBasePath { get; set; } = "/api/";
    public const string DefaultHttpMethod = HttpMethods.Post;
    public static string DefaultUserAgent = "ServiceStack JsonApiClient " + Env.VersionString;

    public JsonApiClient(HttpClient httpClient) : this(httpClient.BaseAddress?.ToString() ?? "/")
    {
        this.HttpClient = httpClient;
    }

    public JsonApiClient(string baseUri)
    {
        this.Format = "json";
        this.Headers = new NameValueCollection();
        this.CookieContainer = new CookieContainer();
        SetBaseUri(this.BaseUri = baseUri);
        JsConfig.InitStatics();
    }

    public static Func<HttpMessageHandler>? GlobalHttpMessageHandlerFactory { get; set; }
    public static Action<JsonApiClient,HttpMessageHandler>? HttpMessageHandlerFilter { get; set; }
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    public HttpClient? HttpClient { get; set; }
    public CookieContainer? CookieContainer { get; set; }

    public ResultsFilterHttpDelegate? ResultsFilter { get; set; }
    public ResultsFilterHttpResponseDelegate? ResultsFilterResponse { get; set; }
    public ExceptionFilterHttpDelegate? ExceptionFilter { get; set; }

    public string BaseUri { get; set; }

    public string? Format { get; private set; }
    public string? RequestCompressionType { get; set; }
    public string? ContentType = MimeTypes.Json;

    public string? SyncReplyBaseUri { get; set; }

    public string? AsyncOneWayBaseUri { get; set; }

    public int Version { get; set; }
    public string? SessionId { get; set; }

    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool AlwaysSendBasicAuthHeader { get; set; }

    public ICredentials? Credentials { get; set; }

    public string? BearerToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? RefreshTokenUri { get; set; }
    public bool EnableAutoRefreshToken { get; set; }

    public bool UseCookies { get; set; } = true;

    /// <summary>
    /// Gets the collection of headers to be added to outgoing requests.
    /// </summary>
    public NameValueCollection? Headers { get; private set; }

    public Action<HttpRequestMessage>? RequestFilter { get; set; }
    public static Action<HttpRequestMessage>? GlobalRequestFilter { get; set; }

    public Action<HttpResponseMessage>? ResponseFilter { get; set; }
    public static Action<HttpResponseMessage>? GlobalResponseFilter { get; set; }

    public UrlResolverDelegate? UrlResolver { get; set; }

    public TypedUrlResolverDelegate? TypedUrlResolver { get; set; }

    /// <summary>
    /// Relative BasePath to use for predefined routes. Set with `UseBasePath` or `WithBasePath()`
    /// Always contains '/' prefix + '/' suffix, e.g. /api/
    /// </summary>
    public string BasePath { get; protected set; } = DefaultBasePath;

    /// <summary>
    /// Replace the Base reply/oneway paths to use a different prefix
    /// </summary>
    public string UseBasePath
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
                this.BasePath = (value[0] != '/' ? '/' + value : value).WithTrailingSlash();
            SetBaseUri(this.BaseUri);
        }
    }

    public JsonApiClient SetBaseUri(string baseUri)
    {
        this.BaseUri = baseUri;
        this.SyncReplyBaseUri = baseUri.CombineWith(BasePath);
        this.AsyncOneWayBaseUri = baseUri.CombineWith(BasePath);
        return this;
    }

    public void SetCredentials(string userName, string password)
    {
        this.UserName = userName;
        this.Password = password;
    }

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
                UseDefaultCredentials = Credentials == null,
                Credentials = Credentials,
                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
            if (CookieContainer != null)
                useHandler.CookieContainer = CookieContainer;
            if (UseCookies) //UseCookies throws in Blazor, only set if true
                useHandler.UseCookies = UseCookies;
                
            handler = useHandler;
        }
            
        var baseUri = new Uri(BaseUri);

        var client = HttpClient = new HttpClient(handler, disposeHandler: HttpMessageHandler == null) { BaseAddress = baseUri };

        HttpMessageHandlerFilter?.Invoke(this, handler);

        if (BearerToken != null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
        else if (AlwaysSendBasicAuthHeader)
            AddBasicAuth(client);

        return HttpClient;
    }

    public void AddHeader(string name, string value)
    {
        Headers ??= new NameValueCollection();
        Headers[name] = value;
    }

    public void DeleteHeader(string name) => Headers?.Remove(name);
    public void ClearHeaders() => Headers?.Clear();

    private int activeAsyncRequests;
    
    internal sealed class CompressContent : HttpContent
    {
        private readonly HttpContent content;
        private readonly IStreamCompressor compressor;
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

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await using var zip = compressor.Compress(stream, leaveOpen:true);
            await content.CopyToAsync(zip).ConfigAwait();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
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

    public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object? request, CancellationToken token = default)
        => SendAsync<TResponse>(httpMethod, absoluteUrl, request, null, token);
    
    public async Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object? request, object? dto, CancellationToken token = default)
    {
        var client = GetHttpClient();
        absoluteUrl = GetAbsoluteUrl(httpMethod, request, absoluteUrl);

        var filterResponse = ResultsFilter?.Invoke(typeof(TResponse), httpMethod, absoluteUrl, request);
        if (filterResponse is TResponse typedResponse)
            return typedResponse;

        var httpReq = CreateRequest(httpMethod, absoluteUrl, request);
        var id = Diagnostics.Client.WriteRequestBefore(httpReq, request, typeof(TResponse));
        Exception? e = null;
        TResponse? response = default; 

        try
        {
            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();

            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (TResponse)(object)httpRes;

            if (httpRes.StatusCode == HttpStatusCode.Unauthorized)
            {
                response = await HandleUnauthorizedResponseAsync<TResponse>(client, httpMethod, absoluteUrl, request, token)
                    .ConfigAwait();
                if (response != null)
                    return response;
            }

            return response = await ConvertToResponseAsync<TResponse>(httpRes, httpMethod, absoluteUrl, request, token)
                .ConfigAwait();
        }
        catch (Exception ex)
        {
            e = ex;
            LogManager.GetLogger(GetType()).Error(ex, nameof(SendAsync) + ": {0}", ex.Message);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.Client.WriteRequestError(id, httpReq, e);
            else
                Diagnostics.Client.WriteRequestAfter(id, httpReq, response);
        }
    }

    public List<TResponse> SendAll<TResponse>(IEnumerable<object> requests)
    {
        var elType = requests.GetType().GetCollectionType();
        var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";
        this.PopulateRequestMetadatas(requests);
        return Send<List<TResponse>>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests);
    }
    
    public virtual void Publish(object request)
    {
        var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
        Send<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), request);
    }

    public void PublishAll(IEnumerable<object> requests)
    {
        var elType = requests.GetType().GetCollectionType();
        var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
        this.PopulateRequestMetadatas(requests);
        Send<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests);
    }

    public virtual TResponse Send<TResponse>(object request)
    {
        if (typeof(TResponse) == typeof(object))
        {
            var result = this.Send(this.GetResponseType(request), request);
            return (TResponse) result;
        }

        var httpMethod = ServiceClientUtils.GetHttpMethod(request.GetType());
        if (httpMethod != null)
        {
            return httpMethod switch {
                HttpMethods.Get => Get<TResponse>(request),
                HttpMethods.Post => Post<TResponse>(request),
                HttpMethods.Put => Put<TResponse>(request),
                HttpMethods.Delete => Delete<TResponse>(request),
                HttpMethods.Patch => Patch<TResponse>(request),
                _ => throw new NotSupportedException("Unknown " + httpMethod),
            };
        }

        httpMethod = DefaultHttpMethod;
        var requestUri = ResolveUrl(httpMethod, UrlResolver == null
            ? this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name
            : this.BasePath + request.GetType().Name);

        return Send<TResponse>(httpMethod, requestUri, request);
    }

    public TResponse Send<TResponse>(string httpMethod, string absoluteUrl, object? request) =>
        Send<TResponse>(httpMethod, absoluteUrl, request, null);
    public TResponse Send<TResponse>(string httpMethod, string absoluteUrl, object? request, object? dto)
    {
        var client = GetHttpClient();
        absoluteUrl = GetAbsoluteUrl(httpMethod, request, absoluteUrl);

        var filterResponse = ResultsFilter?.Invoke(typeof(TResponse), httpMethod, absoluteUrl, request);
        if (filterResponse is TResponse typedResponse)
            return typedResponse;

        var httpReq = CreateRequest(httpMethod, absoluteUrl, request);
        var id = Diagnostics.Client.WriteRequestBefore(httpReq, request ?? dto, typeof(TResponse));
        Exception? e = null;
        TResponse? response = default; 

        try
        {
            var httpRes = client.Send(httpReq);

            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (TResponse)(object) httpRes;

            if (httpRes.StatusCode == HttpStatusCode.Unauthorized)
            {
                response = HandleUnauthorizedResponse<TResponse>(client, httpMethod, absoluteUrl, request);
                if (response != null)
                    return response;
            }

            return response = ConvertToResponse<TResponse>(httpRes, httpMethod, absoluteUrl, request);
        }
        catch (Exception ex)
        {
            e = ex;
            LogManager.GetLogger(GetType()).Error(ex, nameof(Send) + ": {0}", ex.Message);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.Client.WriteRequestError(id, httpReq, e);
            else
                Diagnostics.Client.WriteRequestAfter(id, httpReq, response);
        }
    }

    private TResponse? HandleUnauthorizedResponse<TResponse>(HttpClient client, string httpMethod, string absoluteUrl, object? request)
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
                var accessTokenResponse = this.Post<GetAccessTokenResponse>(uri, refreshDto);

                var accessToken = accessTokenResponse.AccessToken;
                var tokenCookie = this.GetTokenCookie();
                var refreshRequest = CreateRequest(httpMethod, absoluteUrl, request);
                var id = Diagnostics.Client.WriteRequestBefore(refreshRequest, request, typeof(GetAccessTokenResponse));
                Exception? e = null;
                TResponse? response = default;

                try
                {
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

                    var refreshTokenResponse = client.Send(refreshRequest);
                    return response = ConvertToResponse<TResponse>(refreshTokenResponse, httpMethod, absoluteUrl, refreshRequest);
                }
                catch (Exception ex)
                {
                    e = ex;
                    throw;
                }
                finally
                {
                    if (e != null)
                        Diagnostics.Client.WriteRequestError(id, refreshRequest, e);
                    else
                        Diagnostics.Client.WriteRequestAfter(id, refreshRequest, response);
                }
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
            var httpReq = CreateRequest(httpMethod, absoluteUrl, request);

            var id = Diagnostics.Client.WriteRequestBefore(httpReq, request, typeof(TResponse));
            Exception? e = null;
            TResponse? response = default;

            try
            {
                var httpRes = client.Send(httpReq);
                return response = ConvertToResponse<TResponse>(httpRes, httpMethod, absoluteUrl, request);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.Client.WriteRequestError(id, httpReq, e);
                else
                    Diagnostics.Client.WriteRequestAfter(id, httpReq, response);
            }
        }
        
        return default;
    }

    private async Task<TResponse?> HandleUnauthorizedResponseAsync<TResponse>(HttpClient client, string httpMethod, string absoluteUrl, object? request,
        CancellationToken token=default)
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

                var id = Diagnostics.Client.WriteRequestBefore(refreshRequest, request, typeof(GetAccessTokenResponse));
                Exception? e = null;
                TResponse? response = default;

                try
                {
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
                    return response = await ConvertToResponseAsync<TResponse>(
                        refreshTokenResponse, httpMethod, absoluteUrl, refreshRequest, token).ConfigAwait();
                }
                catch (Exception ex)
                {
                    e = ex;
                    throw;
                }
                finally
                {
                    if (e != null)
                        Diagnostics.Client.WriteRequestError(id, refreshRequest, e);
                    else
                        Diagnostics.Client.WriteRequestAfter(id, refreshRequest, response);
                }
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
            var httpReq = CreateRequest(httpMethod, absoluteUrl, request);

            var id = Diagnostics.Client.WriteRequestBefore(httpReq, request, typeof(TResponse));
            Exception? e = null;
            TResponse? response = default;

            try
            {
                var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();
                return response = await ConvertToResponseAsync<TResponse>(httpRes, httpMethod, absoluteUrl, request, token).ConfigAwait();
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.Client.WriteRequestError(id, httpReq, e);
                else
                    Diagnostics.Client.WriteRequestAfter(id, httpReq, response);
            }
        }
        
        return default;
    }
    
    private string GetAbsoluteUrl(string httpMethod, object? request, string relativeOrAbsoluteUrl)
    {
        var absoluteUrl = ToAbsoluteUrl(relativeOrAbsoluteUrl);
        
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
            var log = LogManager.GetLogger(typeof(JsonApiClient));
            if (log.IsDebugEnabled)
                log.Debug("Could not parse URL: " + absoluteUrl, ex);
        }

        return absoluteUrl;
    }

    private HttpRequestMessage CreateRequest(string httpMethod, string absoluteUrl, object? request)
    {
        this.PopulateRequestMetadata(request);

        var httpReq = new HttpRequestMessage(new HttpMethod(httpMethod), absoluteUrl);

        if (Headers != null)
        {
            foreach (var name in Headers.AllKeys)
            {
                if (name != null)
                    httpReq.Headers.Add(name, Headers[name]);
            }
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

    private async Task<TResponse> ConvertToResponseAsync<TResponse>(HttpResponseMessage httpRes, string httpMethod, string absoluteUrl, object? request, CancellationToken token=default)
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
            var result = await ThrowIfErrorAsync(() => httpRes.Content.ReadAsStringAsync(token), httpRes, request, absoluteUrl).ConfigAwait();
            var response = (TResponse) (object) result;
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
            return response;
        }
        if (typeof(TResponse) == typeof(byte[]))
        {
            var result = await ThrowIfErrorAsync(() => httpRes.Content.ReadAsByteArrayAsync(token), httpRes, request, absoluteUrl).ConfigAwait();
            var response = (TResponse) (object) result;
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
            return response;
        }
        if (typeof(TResponse) == typeof(Stream))
        {
            var result = await ThrowIfErrorAsync(() => httpRes.Content.ReadAsStreamAsync(token), httpRes, request, absoluteUrl).ConfigAwait();
            var response = (TResponse) (object) result;
            ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
            return response;
        }

        var json = await ThrowIfErrorAsync(() => httpRes.Content.ReadAsStringAsync(token), httpRes, request, absoluteUrl).ConfigAwait();
        var obj = json.FromJson<TResponse>();
        ResultsFilterResponse?.Invoke(httpRes, obj, httpMethod, absoluteUrl, request);
        return obj;
    }

    private TResponse ConvertToResponse<TResponse>(HttpResponseMessage httpRes, string httpMethod, string absoluteUrl, object? request)
    {
        ApplyWebResponseFilters(httpRes);

        if (!httpRes.IsSuccessStatusCode && ExceptionFilter != null)
        {
            var cachedResponse = ExceptionFilter(httpRes, absoluteUrl, typeof(TResponse));
            if (cachedResponse is TResponse filterResponse)
                return filterResponse;
        }

        TResponse? response = default;
        if (typeof(TResponse) == typeof(string))
            response = (TResponse) (object) ThrowIfError(() => httpRes.Content.ReadAsString(), httpRes, request, absoluteUrl);
        if (typeof(TResponse) == typeof(byte[]))
            response = (TResponse) (object) ThrowIfError(() => httpRes.Content.ReadAsByteArray(), httpRes, request, absoluteUrl);
        if (typeof(TResponse) == typeof(Stream))
            response = (TResponse) (object) ThrowIfError(() => httpRes.Content.ReadAsStream(), httpRes, request, absoluteUrl);

        if (response == null)
        {
            var json = ThrowIfError(() => httpRes.Content.ReadAsString(), httpRes, request, absoluteUrl);
            response = json.FromJson<TResponse>();
        }

        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, absoluteUrl, request);
        return response;
    }

    private void ApplyWebRequestFilters(HttpRequestMessage httpReq)
    {
        RequestFilter?.Invoke(httpReq);
        GlobalRequestFilter?.Invoke(httpReq);
    }

    private void ApplyWebResponseFilters(HttpResponseMessage httpRes)
    {
        ResponseFilter?.Invoke(httpRes);
        GlobalResponseFilter?.Invoke(httpRes);
    }

    private async Task<TResponse> ThrowIfErrorAsync<TResponse>(Func<Task<TResponse>> fn, HttpResponseMessage httpRes, object? request, string requestUri)
    {
        Interlocked.Decrement(ref activeAsyncRequests);

        TResponse response;
        try
        {
            response = await fn().ConfigAwait();
        }
        catch (Exception e)
        {
            throw CreateException(httpRes, e);
        }

        if (!httpRes.IsSuccessStatusCode)
            ThrowResponseTypeException<TResponse>(httpRes, request, requestUri, response);

        return response;
    }

    private TResponse ThrowIfError<TResponse>(Func<TResponse> fn, HttpResponseMessage httpRes, object? request, string requestUri)
    {
        TResponse response;
        try
        {
            response = fn();
        }
        catch (Exception e)
        {
            throw CreateException(httpRes, e);
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
        where T : class
    {
        ResultsFilterResponse?.Invoke(httpRes, response, httpMethod, requestUri, request);
        return response;
    }

    private static WebServiceException CreateException(HttpResponseMessage httpRes, Exception ex) => new();

    readonly ConcurrentDictionary<Type, Action<HttpResponseMessage, object?, string, object?>> responseHandlers = new();

    private void ThrowResponseTypeException<TResponse>(HttpResponseMessage httpRes, object? request, string requestUri, object? response)
    {
        var responseType = WebRequestUtils.GetErrorResponseDtoType<TResponse>(request);
        if (!responseHandlers.TryGetValue(responseType, out var responseHandler))
        {
            var mi = GetType().GetInstanceMethod("ThrowWebServiceException")
                .MakeGenericMethod(responseType);

            responseHandler = (Action<HttpResponseMessage, object?, string, object?>)mi.CreateDelegate(
                typeof(Action<HttpResponseMessage, object?, string, object?>), this);

            responseHandlers[responseType] = responseHandler;
        }
        responseHandler(httpRes, request, requestUri, response);
    }

    public static byte[]? GetResponseBytes(object response)
    {
        if (response is Stream stream)
            return stream.ReadFully();

        if (response is byte[] bytes)
            return bytes;

        var str = response as string;
        return str?.ToUtf8Bytes();
    }

    public static WebServiceException ToWebServiceException(
        HttpResponseMessage httpRes, object response, Func<Stream, object?> parseDtoFn)
    {
        var log = LogManager.GetLogger(typeof(JsonApiClient));
        if (log.IsDebugEnabled)
        {
            log.DebugFormat("Status Code : {0}", httpRes.StatusCode);
            log.DebugFormat("Status Description : {0}", httpRes.ReasonPhrase);
        }

        var serviceEx = new WebServiceException(httpRes.ReasonPhrase)
        {
            StatusCode = (int)httpRes.StatusCode,
            StatusDescription = httpRes.ReasonPhrase,
            ResponseHeaders = httpRes.Headers.ToWebHeaderCollection(),
        };

        try
        {
            var contentType = httpRes.GetContentType();

            var bytes = GetResponseBytes(response);
            if (bytes != null)
            {
                serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                if (contentType.MatchesContentType(MimeTypes.Json) || 
                    (string.IsNullOrEmpty(contentType) && !string.IsNullOrEmpty(serviceEx.ResponseBody) && serviceEx.ResponseBody.AsSpan().TrimStart().StartsWith("{")))
                {
                    var stream = MemoryStreamFactory.GetStream(bytes);
                    serviceEx.ResponseDto = parseDtoFn?.Invoke(stream);

                    if (stream.CanRead)
                        stream.Dispose(); //alt ms throws when you dispose twice
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

        var status = webEx.ResponseStatus;
        if (status is { StackTrace: null } && Diagnostics.IncludeStackTrace)
            status.StackTrace = Environment.StackTrace;

        throw webEx;
    }

    public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto) =>
        SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto);
    public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto, token);

    public Task<TResponse> GetAsync<TResponse>(object requestDto) =>
        SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto);
    public Task<TResponse> GetAsync<TResponse>(object requestDto, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto, token);

    public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl) =>
        SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);
    public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null, token);

    public Task GetAsync(IReturnVoid requestDto) =>
        SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto);
    public Task GetAsync(IReturnVoid requestDto, CancellationToken token) =>
        SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto, token);


    public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto) =>
        SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto);
    public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto, token);

    public Task<TResponse> DeleteAsync<TResponse>(object requestDto) =>
        SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto);
    public Task<TResponse> DeleteAsync<TResponse>(object requestDto, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto, token);

    public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl) =>
        SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);
    public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token) =>
        SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null, token);

    public Task DeleteAsync(IReturnVoid requestDto) =>
        SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto);
    public Task DeleteAsync(IReturnVoid requestDto, CancellationToken token) =>
        SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto, token);


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
        return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, requestDto, token);
    }

    public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto, CancellationToken token = default)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return SendAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, requestDto, token);
    }

    public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto, CancellationToken token = default)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, requestDto, token);
    }

    public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request, CancellationToken token = default)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? request : null;
        return SendAsync<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody, request, token);
    }

    public string? GetHttpMethod(object request) => ServiceClientUtils.GetHttpMethod(request.GetType());

    public void SendOneWay(object request) => Publish(request);

    public void SendOneWay(string relativeOrAbsoluteUrl, object request)
    {
        var httpMethod = ServiceClientUtils.GetHttpMethod(request.GetType()) ?? DefaultHttpMethod;
        var absoluteUri = ToAbsoluteUrl(ResolveUrl(httpMethod, relativeOrAbsoluteUrl));
        Send<byte[]>(httpMethod, absoluteUri, request);
    }

    public void SendAllOneWay(IEnumerable<object> requests)
    {
        var elType = requests.GetType().GetCollectionType();
        var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
        var absoluteUri = ToAbsoluteUrl(ResolveUrl(HttpMethods.Post, requestUri));
        this.PopulateRequestMetadatas(requests);
        Send<byte[]>(HttpMethods.Post, absoluteUri, requests);
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

    public TResponse Get<TResponse>(IReturn<TResponse> requestDto) =>
        Send<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto);
    public TResponse Get<TResponse>(object requestDto) =>
        Send<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto);

    public TResponse Get<TResponse>(string relativeOrAbsoluteUrl) =>
        Send<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);

    public virtual IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto)
    {
        var query = (IQuery)queryDto;
        if (query.Include == null || query.Include.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (!string.IsNullOrEmpty(query.Include))
                query.Include += ",";
            query.Include += "Total";
        }

        QueryResponse<TResponse> response;
        do
        {
            response = Send<QueryResponse<TResponse>>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, queryDto), null, queryDto);
            foreach (var result in response.Results)
            {
                yield return result;
            }
            query.Skip = query.Skip.GetValueOrDefault(0) + response.Results.Count;
        }
        while (response.Results.Count + response.Offset < response.Total);
    }


    public void Get(IReturnVoid requestDto) =>
        Send<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, requestDto);


    public TResponse Delete<TResponse>(IReturn<TResponse> requestDto) =>
        Send<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto);

    public TResponse Delete<TResponse>(object requestDto) =>
        Send<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto);

    public TResponse Delete<TResponse>(string relativeOrAbsoluteUrl) =>
        Send<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);

    public void Delete(IReturnVoid requestDto) =>
        Send<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, requestDto);


    public TResponse Post<TResponse>(IReturn<TResponse> requestDto) =>
        Send<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);

    public TResponse Post<TResponse>(object requestDto) =>
        Send<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);

    public TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request) =>
        Send<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request);

    public void Post(IReturnVoid requestDto) =>
        Send<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);


    public TResponse Put<TResponse>(IReturn<TResponse> requestDto) =>
        Send<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);

    public TResponse Put<TResponse>(object requestDto) =>
        Send<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);

    public TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request) =>
        Send<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request);

    public void Put(IReturnVoid requestDto) =>
        Send<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);


    public TResponse Patch<TResponse>(IReturn<TResponse> requestDto) =>
        Send<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);

    public TResponse Patch<TResponse>(object requestDto) =>
        Send<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);

    public TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object request) =>
        Send<TResponse>(HttpMethods.Patch, ResolveUrl(HttpMethods.Patch, relativeOrAbsoluteUrl), request);

    public void Patch(IReturnVoid requestDto) =>
        Send<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        
        
    public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return Send<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
    }

    public TResponse CustomMethod<TResponse>(string httpVerb, object requestDto)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        return Send<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
    }

    public void CustomMethod(string httpVerb, IReturnVoid requestDto)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
        Send<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
    }

    public TResponse CustomMethod<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request)
    {
        if (!HttpMethods.Exists(httpVerb))
            throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

        var requestBody = HttpUtils.HasRequestBody(httpVerb) ? request : null;
        return Send<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody);
    }

    
    
    public virtual async Task<TResponse> PostFileAsync<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string? mimeType = null, string fieldName = "file", CancellationToken token = default)
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
    
    public virtual TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string? mimeType = null, string fieldName = "file")
    {
        using var content = new MultipartFormDataContent();
        var fileBytes = fileToUpload.ReadFully();
        using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = fieldName,
            FileName = fileName
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
        content.Add(fileContent, fieldName, fileName);

        var result = Send<TResponse>(HttpMethods.Post,
            ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), content);
        return result;
    }
    
    public virtual TResponse PutFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string? mimeType = null, string fieldName = "file")
    {
        using var content = new MultipartFormDataContent();
        var fileBytes = fileToUpload.ReadFully();
        using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = fieldName,
            FileName = fileName
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
        content.Add(fileContent, fieldName, fileName);

        var result = Send<TResponse>(HttpMethods.Put,
            ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), content);
        return result;
    }

    public Task<TResponse> PostFileWithRequestAsync<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "file", CancellationToken token = default)
    {
        return PostFileWithRequestAsync<TResponse>(ResolveTypedUrl(
            GetHttpMethod(request) ?? HttpMethods.Post, request), fileToUpload, fileName, request, fieldName, token);
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
            if (value != null)
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

    public TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "file")
    {
        return PostFileWithRequest<TResponse>(ResolveTypedUrl(GetHttpMethod(request) ?? HttpMethods.Post, request), fileToUpload, fileName, request, 
            fieldName:fieldName);
    }

    public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
        object request, string fieldName = "file")
    {
        var queryString = QueryStringSerializer.SerializeToString(request);
        var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);

        using var content = new MultipartFormDataContent();

        foreach (string key in nameValueCollection)
        {
            var value = nameValueCollection[key];
            if (value != null)
                content.Add(new StringContent(value), $"\"{key}\"");
        }

        var fileBytes = fileToUpload.ReadFully();
        using var fileContent = new ByteArrayContent(fileBytes, 0, fileBytes.Length);
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = fieldName,
            FileName = fileName
        };
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MimeTypes.GetMimeType(fileName));
        content.Add(fileContent, fieldName, fileName);

        var httpMethod = GetHttpMethod(request) ?? HttpMethods.Post;
        var result = Send<TResponse>(httpMethod, ResolveUrl(httpMethod, relativeOrAbsoluteUrl), content);
        return result;
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
            if (value != null)
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
            var result = await SendAsync<TResponse>(GetHttpMethod(request) ?? HttpMethods.Post, requestUri, content, token).ConfigAwait();
            return result;
        }
        finally
        {
            foreach (var d in disposables) d.Dispose();
        }
    }

    public TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files)
    {
        return PostFilesWithRequest<TResponse>(ResolveTypedUrl(GetHttpMethod(request) ?? HttpMethods.Post, request), request, files.ToArray());
    }

    public TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files)
    {
        return PostFilesWithRequest<TResponse>(ResolveUrl(GetHttpMethod(request) ?? HttpMethods.Post, relativeOrAbsoluteUrl), request, files.ToArray());
    }

    public virtual TResponse PostFilesWithRequest<TResponse>(string requestUri, object request, UploadFile[] files)
    {
        var queryString = QueryStringSerializer.SerializeToString(request);
        var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);

        using var content = new MultipartFormDataContent();

        foreach (string key in nameValueCollection)
        {
            var value = nameValueCollection[key];
            if (value != null)
                content.Add(new StringContent(value), $"\"{key}\"");
        }

        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            content.AddFile(
                fieldName: file.FieldName ?? $"upload{i}", 
                fileName: file.FileName ?? $"upload{i}.bin", 
                fileContents: file.Stream, 
                mimeType:file.ContentType);
        }

        try
        {
            var result = Send<TResponse>(GetHttpMethod(request) ?? HttpMethods.Post, requestUri, content);
            return result;
        }
        finally
        {
            foreach (var uploadFile in files) 
                uploadFile.Stream.Dispose();
        }
    }

    public void CancelAsync() => throw new NotSupportedException("Pass CancellationToken when calling each async API");

    public void Dispose()
    {
        HttpClient?.Dispose();
        HttpClient = null;
    }

    public TResponse SendForm<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, MultipartFormDataContent request)
    {
        var client = GetHttpClient();
        var absoluteUrl = GetAbsoluteUrl(httpMethod, request:null, relativeOrAbsoluteUrl);

        var filterResponse = ResultsFilter?.Invoke(typeof(TResponse), httpMethod, absoluteUrl, request:null);
        if (filterResponse is TResponse typedResponse)
            return typedResponse;

        var httpReq = CreateRequest(httpMethod, absoluteUrl, request:request);
        var id = Diagnostics.Client.WriteRequestBefore(httpReq, request, typeof(TResponse));
        Exception? e = null;
        TResponse? response = default; 

        try
        {
            var httpRes = client.Send(httpReq);

            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (TResponse)(object) httpRes;

            if (httpRes.StatusCode == HttpStatusCode.Unauthorized)
            {
                response = HandleUnauthorizedResponse<TResponse>(client, httpMethod, absoluteUrl, request:request);
                if (response != null)
                    return response;
            }

            return ConvertToResponse<TResponse>(httpRes, httpMethod, absoluteUrl, request);
        }
        catch (Exception ex)
        {
            e = ex;
            LogManager.GetLogger(GetType()).Error(ex, nameof(SendForm) + ": {0}", ex.Message);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.Client.WriteRequestError(id, httpReq, e);
            else
                Diagnostics.Client.WriteRequestAfter(id, httpReq, response);
        }
    }
    
    public ApiResult<TResponse> ApiForm<TResponse>(string relativeOrAbsoluteUrl, MultipartFormDataContent request)
    {
        try
        {
            var result = SendForm<TResponse>(HttpMethods.Post, relativeOrAbsoluteUrl, request);
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    public ApiResult<TResponse> ApiForm<TResponse>(IReturn<TResponse> request, MultipartFormDataContent body)
    {
        try
        {
            body.AddParams(request);
            var relativeOrAbsoluteUrl = request.GetType().ToApiUrl();
            var result = SendForm<TResponse>(ServiceClientUtils.GetHttpMethod(request.GetType()) ?? HttpMethods.Post, relativeOrAbsoluteUrl, body);
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    public async Task<TResponse> SendFormAsync<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, MultipartFormDataContent request, CancellationToken token=default)
    {
        var client = GetHttpClient();
        var absoluteUrl = GetAbsoluteUrl(httpMethod, request:null, relativeOrAbsoluteUrl);

        var filterResponse = ResultsFilter?.Invoke(typeof(TResponse), httpMethod, absoluteUrl, request:null);
        if (filterResponse is TResponse typedResponse)
            return typedResponse;

        var httpReq = CreateRequest(httpMethod, absoluteUrl, request:request);
        var id = Diagnostics.Client.WriteRequestBefore(httpReq, request, typeof(TResponse));
        Exception? e = null;
        TResponse? response = default; 

        try
        {
            var httpRes = await client.SendAsync(httpReq, token).ConfigAwait();

            if (typeof(TResponse) == typeof(HttpResponseMessage))
                return (TResponse)(object) httpRes;

            if (httpRes.StatusCode == HttpStatusCode.Unauthorized)
            {
                response = await HandleUnauthorizedResponseAsync<TResponse>(client, httpMethod, absoluteUrl, request:request, token).ConfigAwait();
                if (response != null)
                    return response;
            }

            return ConvertToResponse<TResponse>(httpRes, httpMethod, absoluteUrl, request);
        }
        catch (Exception ex)
        {
            e = ex;
            LogManager.GetLogger(GetType()).Error(ex, nameof(SendFormAsync) + ": {0}", ex.Message);
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.Client.WriteRequestError(id, httpReq, e);
            else
                Diagnostics.Client.WriteRequestAfter(id, httpReq, response);
        }
    }

    public async Task<ApiResult<TResponse>> ApiFormAsync<TResponse>(string method, string relativeOrAbsoluteUrl, MultipartFormDataContent request, CancellationToken token = default)
    {
        try
        {
            var result = await SendFormAsync<TResponse>(method, relativeOrAbsoluteUrl, request, token).ConfigAwait();
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    public async Task<ApiResult<TResponse>> ApiFormAsync<TResponse>(string relativeOrAbsoluteUrl, MultipartFormDataContent request, CancellationToken token = default)
    {
        try
        {
            var result = await SendFormAsync<TResponse>(HttpMethods.Post, relativeOrAbsoluteUrl, request, token).ConfigAwait();
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    public async Task<ApiResult<TResponse>> ApiFormAsync<TResponse>(IReturn<TResponse> request, MultipartFormDataContent body, CancellationToken token=default)
    {
        try
        {
            body.AddParams(request);
            var relativeOrAbsoluteUrl = request.GetType().ToApiUrl();
            var result = await SendFormAsync<TResponse>(ServiceClientUtils.GetHttpMethod(request.GetType()) ?? HttpMethods.Post, relativeOrAbsoluteUrl, body, token).ConfigAwait();
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    public Task<TResponse> SendFormAsync<TResponse>(object requestDto, MultipartFormDataContent formData, CancellationToken token = default) =>
        SendFormAsync<TResponse>(ServiceClientUtils.GetHttpMethod(requestDto.GetType()) ?? HttpMethods.Post, requestDto.GetType().ToApiUrl(), formData, token);
}


#endif