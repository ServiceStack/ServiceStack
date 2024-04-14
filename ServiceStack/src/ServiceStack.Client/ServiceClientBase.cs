#pragma warning disable SYSLIB0014

// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{

    /**
     * Need to provide async request options
     * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
     */
    public abstract class ServiceClientBase : IServiceClient, IMessageProducer, IHasCookieContainer, IServiceClientMeta
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ServiceClientBase));

        private AuthenticationInfo authInfo = null;

        /// <summary>
        /// The request filter is called before any request.
        /// This request filter is executed globally.
        /// </summary>
        public static Action<HttpWebRequest> GlobalRequestFilter
        {
            get => globalRequestFilter;
            set
            {
                globalRequestFilter = value;
                AsyncServiceClient.GlobalRequestFilter = value;
            }
        }
        private static Action<HttpWebRequest> globalRequestFilter;

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// This response action is executed globally.
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        public static Action<HttpWebResponse> GlobalResponseFilter
        {
            get => globalResponseFilter;
            set
            {
                globalResponseFilter = value;
                AsyncServiceClient.GlobalResponseFilter = value;
            }
        }
        private static Action<HttpWebResponse> globalResponseFilter;

        /// <summary>
        /// Gets the collection of headers to be added to outgoing requests.
        /// </summary>
        public NameValueCollection Headers { get; private set; }

        public const string DefaultHttpMethod = HttpMethods.Post;
        public static string DefaultUserAgent = "ServiceStackClient/" + Env.VersionString;

#if NET6_0_OR_GREATER
        public System.Net.Http.HttpClient HttpClient { get; set; } 
#endif

        readonly AsyncServiceClient asyncClient;

        protected ServiceClientBase()
        {
            this.HttpMethod = DefaultHttpMethod;
            this.Headers = new NameValueCollection();
            var cookies = new CookieContainer();
#if NET6_0_OR_GREATER
            this.HttpClient = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler {
                CookieContainer = cookies,
            }, disposeHandler: true);
#endif

            asyncClient = new AsyncServiceClient
            {
                ContentType = ContentType,
                StreamSerializer = AsyncSerializeToStream,
                StreamDeserializer = AsyncDeserializeFromStream,
                UserName = this.UserName,
                Password = this.Password,
                RequestFilter = this.RequestFilter,
                ResponseFilter = this.ResponseFilter,
                ResultsFilter = this.ResultsFilter,
                Headers = this.Headers,
#if NET6_0_OR_GREATER
                HttpClient = this.HttpClient,
#endif
            };
            this.CookieContainer = cookies;
            this.StoreCookies = true; //leave
            this.UserAgent = DefaultUserAgent;
            this.EnableAutoRefreshToken = true;

            asyncClient.ShareCookiesWithBrowser = this.ShareCookiesWithBrowser = true;

            JsConfig.InitStatics();
        }

        protected ServiceClientBase(string syncReplyBaseUri, string asyncOneWayBaseUri)
            : this()
        {
            this.SyncReplyBaseUri = syncReplyBaseUri;
            this.AsyncOneWayBaseUri = asyncOneWayBaseUri;
        }

        /// <summary>
        /// Sets all baseUri properties, using the Format property for the SyncReplyBaseUri and AsyncOneWayBaseUri
        /// </summary>
        /// <param name="baseUri">Base URI of the service</param>
        public void SetBaseUri(string baseUri)
        {
            this.BasePath = "/" + Format + "/reply/";
            this.BaseUri = baseUri;
            this.asyncClient.BaseUri = baseUri;
            this.SyncReplyBaseUri = baseUri.WithTrailingSlash() + Format + "/reply/";
            this.AsyncOneWayBaseUri = baseUri.WithTrailingSlash() + Format + "/oneway/";
        }

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

        /// <summary>
        /// Whether to Accept Gzip,Deflate Content-Encoding and to auto decompress responses
        /// </summary>
        public bool DisableAutoCompression
        {
            get => disableAutoCompression;
            set
            {
                disableAutoCompression = value;
                asyncClient.DisableAutoCompression = value;
            }
        }
        private bool disableAutoCompression;


        private string requestCompressionType;
        public string RequestCompressionType
        {
            get => requestCompressionType;
            set
            {
                requestCompressionType = value;
                asyncClient.RequestCompressionType = value;
            }
        }

        /// <summary>
        /// The user name for basic authentication
        /// </summary>
        public string UserName
        {
            get => username;
            set
            {
                username = value;
                asyncClient.UserName = value;
            }
        }
        private string username;

        /// <summary>
        /// The password for basic authentication
        /// </summary>
        public string Password
        {
            get => password;
            set
            {
                password = value;
                asyncClient.Password = value;
            }
        }
        private string password;

        /// <summary>
        /// Sets the username and the password for basic authentication.
        /// </summary>
        public void SetCredentials(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        /// <summary>
        /// The Authorization Bearer Token to send with this request
        /// </summary>
        public string BearerToken
        {
            get => bearerToken;
            set
            {
                bearerToken = value;
                asyncClient.BearerToken = value;
            }
        }
        private string bearerToken;

        public string BaseUri { get; set; }

        public abstract string Format { get; }

        public string SyncReplyBaseUri { get; set; }

        public string AsyncOneWayBaseUri { get; set; }

        private int version;
        public int Version
        {
            get => version;
            set
            {
                this.version = value;
                this.asyncClient.Version = value;
            }
        }
        
        private string sessionId;
        public string SessionId
        {
            get => sessionId;
            set
            {
                sessionId = value;
                asyncClient.SessionId = value;
            }
        }

        private string userAgent;
        public string UserAgent
        {
            get => userAgent;
            set
            {
                userAgent = value;
                asyncClient.UserAgent = value;
            }
        }

        public TimeSpan? Timeout
        {
            get => this.timeout;
            set
            {
                this.timeout = value;
                this.asyncClient.Timeout = value;
            }
        }
        private TimeSpan? timeout;

        public TimeSpan? ReadWriteTimeout
        {
            get => this.readWriteTimeout;
            set
            {
                this.readWriteTimeout = value;
                // TODO implement ReadWriteTimeout in asyncClient
                //this.asyncClient.ReadWriteTimeout = value;
            }
        }
        private TimeSpan? readWriteTimeout;

        public virtual string Accept => ContentType;

        public abstract string ContentType { get; }

        public string HttpMethod { get; set; }

        public bool EmulateHttpViaPost
        {
            get => asyncClient.EmulateHttpViaPost;
            set => asyncClient.EmulateHttpViaPost = value;
        }

        public ProgressDelegate OnDownloadProgress
        {
            get => asyncClient.OnDownloadProgress;
            set => asyncClient.OnDownloadProgress = value;
        }

        public ProgressDelegate OnUploadProgress
        {
            get => asyncClient.OnUploadProgress;
            set => asyncClient.OnUploadProgress = value;
        }

        private bool shareCookiesWithBrowser;
        public bool ShareCookiesWithBrowser
        {
            get => this.shareCookiesWithBrowser;
            set => asyncClient.ShareCookiesWithBrowser = this.shareCookiesWithBrowser = value;
        }

        private IWebProxy proxy;
        public IWebProxy Proxy
        {
            get => this.proxy;
            set => asyncClient.Proxy = this.proxy = value;
        }

        /// <summary>
        /// Gets or sets authentication information for the request.
        /// Warning: It's recommend to use <see cref="UserName"/> and <see cref="Password"/> for basic auth.
        /// This property is only used for IIS level authentication.
        /// </summary>
        public ICredentials Credentials
        {
            get => this.credentials;
            set
            {
                this.credentials = value;
                this.asyncClient.Credentials = value;
            }
        }
        private ICredentials credentials;

        /// <summary>
        /// Determines if the basic auth header should be sent with every request.
        /// By default, the basic auth header is only sent when "401 Unauthorized" is returned.
        /// </summary>
        public bool AlwaysSendBasicAuthHeader
        {
            get => alwaysSendBasicAuthHeader;
            set => asyncClient.AlwaysSendBasicAuthHeader = alwaysSendBasicAuthHeader = value;
        }
        private bool alwaysSendBasicAuthHeader;

        /// <summary>
        /// Specifies if cookies should be stored
        /// </summary>
        public bool StoreCookies
        {
            get => storeCookies;
            set => asyncClient.StoreCookies = storeCookies = value;
        }
        private bool storeCookies;

        public CookieContainer CookieContainer
        {
            get => cookieContainer;
            set => asyncClient.CookieContainer = cookieContainer = value;
        }
        private CookieContainer cookieContainer;

        public bool AllowAutoRedirect { get; set; } = true;

        /// <summary>
        /// Called before request resend, when the initial request required authentication
        /// </summary>
        public Action OnAuthenticationRequired
        {
            get => onAuthenticationRequired;
            set
            {
                onAuthenticationRequired = value;
                asyncClient.OnAuthenticationRequired = value;
            }
        }
        private Action onAuthenticationRequired;

        /// <summary>
        /// If a request fails with a 401 Unauthorized and a BearerToken is present the client
        /// will automatically fetch a new AccessToken using this RefreshToken and retry the request
        /// </summary>
        public string RefreshToken
        {
            get => refreshToken;
            set
            {
                refreshToken = value;
                asyncClient.RefreshToken = value;
            }
        }
        private string refreshToken;

        /// <summary>
        /// Send the Request to get the AccessToken with the RefreshToken at a non-default location
        /// </summary>
        public string RefreshTokenUri
        {
            get => refreshTokenUri;
            set
            {
                refreshTokenUri = value;
                asyncClient.RefreshTokenUri = value;
            }
        }
        private string refreshTokenUri;

        /// <summary>
        /// Whether to enable auto refreshing token of JWT Tokens from Refresh Tokens
        /// </summary>
        public bool EnableAutoRefreshToken
        {
            get => enableAutoRefreshToken;
            set
            {
                enableAutoRefreshToken = value;
                asyncClient.EnableAutoRefreshToken = value;
            }
        }
        private bool enableAutoRefreshToken;

        /// <summary>
        /// The request filter is called before any request.
        /// This request filter only works with the instance where it was set (not global).
        /// </summary>
        public Action<HttpWebRequest> RequestFilter
        {
            get => requestFilter;
            set
            {
                requestFilter = value;
                asyncClient.RequestFilter = value;
            }
        }
        private Action<HttpWebRequest> requestFilter;

        /// <summary>
        /// The ResultsFilter is called before the Request is sent allowing you to return a cached response.
        /// </summary>
        public ResultsFilterDelegate ResultsFilter
        {
            get => resultsFilter;
            set
            {
                resultsFilter = value;
                asyncClient.ResultsFilter = value;
            }
        }
        private ResultsFilterDelegate resultsFilter;

        /// <summary>
        /// The ResultsFilterResponse is called before returning the response allowing responses to be cached.
        /// </summary>
        public ResultsFilterResponseDelegate ResultsFilterResponse
        {
            get => resultsFilterResponse;
            set
            {
                resultsFilterResponse = value;
                asyncClient.ResultsFilterResponse = value;
            }
        }
        private ResultsFilterResponseDelegate resultsFilterResponse;

        /// <summary>
        /// Called with requestUri, ResponseType when server returns 304 NotModified
        /// </summary>
        public ExceptionFilterDelegate ExceptionFilter
        {
            get => exceptionFilter;
            set
            {
                exceptionFilter = value;
                asyncClient.ExceptionFilter = value;
            }
        }
        private ExceptionFilterDelegate exceptionFilter;

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        public Action<HttpWebResponse> ResponseFilter
        {
            get => responseFilter;
            set
            {
                responseFilter = value;
                asyncClient.ResponseFilter = value;
            }
        }
        private Action<HttpWebResponse> responseFilter;

        public StringBuilder HttpLog
        {
            get => httpLog;
            set
            {
                httpLog = value;
                asyncClient.HttpLog = value;
            }
        }
        private StringBuilder httpLog;
        public Action<StringBuilder> HttpLogFilter { get; set; }

        public void CaptureHttp(bool print = false, bool log = false, bool clear = true)
        {
            CaptureHttp(sb => {
                if (print)
                    PclExport.Instance.WriteLine(sb.ToString());
                if (log && ServiceClientBase.log?.IsDebugEnabled == true)
                    ServiceClientBase.log.Debug(sb.ToString());
                if (clear)
                    sb.Clear();
            });
        }

        public void CaptureHttp(Action<StringBuilder> httpFilter)
        {
            HttpLog ??= new StringBuilder();
            asyncClient.HttpLogFilter = HttpLogFilter = httpFilter;
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

        internal void AsyncSerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            SerializeRequestToStream(request, stream);
        }

        public abstract void SerializeToStream(IRequest req, object request, Stream stream);

        public abstract T DeserializeFromStream<T>(Stream stream);

        public abstract StreamDeserializerDelegate StreamDeserializer { get; }

        internal object AsyncDeserializeFromStream(Type type, Stream fromStream)
        {
            return StreamDeserializer(type, fromStream);
        }

        protected T Deserialize<T>(string text)
        {
            using (var ms = MemoryStreamFactory.GetStream(text.ToUtf8Bytes()))
            {
                return DeserializeFromStream<T>(ms);
            }
        }

        public virtual List<TResponse> SendAll<TResponse>(IEnumerable<object> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";
            this.PopulateRequestMetadatas(requests);
            var client = SendRequest(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests);

            try
            {
                var webResponse = client.GetResponse();
                return HandleResponse<List<TResponse>>(webResponse);
            }
            catch (Exception ex)
            {
                if (!HandleResponseException(ex,
                    requests,
                    requestUri,
                    () => SendRequest(HttpMethods.Post, requestUri, requests),
                    c => c.GetResponse(),
                    out List<TResponse> response))
                {
                    throw;
                }

                return response;
            }
        }

        public virtual TResponse Send<TResponse>(object request)
        {
            if (typeof(TResponse) == typeof(object))
                return (TResponse)this.Send(this.GetResponseType(request), request);

            var httpMethod = GetHttpMethod(request);
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

            httpMethod = HttpMethod ?? DefaultHttpMethod;
            var requestUri = ResolveUrl(httpMethod, UrlResolver == null
                ? this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name
                : this.BasePath + request.GetType().Name);

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, requestUri, request);
                if (response is TResponse res)
                    return res;
            }

            var client = SendRequest(httpMethod, requestUri, request);

            try
            {
                var webResponse = client.GetResponse();
                ApplyWebResponseFilters(webResponse);

                var response = GetResponse<TResponse>(webResponse);
                ResultsFilterResponse?.Invoke(webResponse, response, httpMethod, requestUri, request);

                DisposeIfRequired<TResponse>(webResponse);

                return response;
            }
            catch (Exception ex)
            {
                if (!HandleResponseException(ex,
                    request,
                    requestUri,
                    () => SendRequest(HttpMethods.Post, requestUri, request),
                    c => c.GetResponse(),
                    out TResponse response))
                {
                    throw;
                }

                return response;
            }
        }

        /// <summary>
        /// Called by Send method if an exception occurs, for instance a System.Net.WebException because the server
        /// returned an HTTP error code. Override if you want to handle specific exceptions or always want to parse the
        /// response to a custom ErrorResponse DTO type instead of ServiceStack's ErrorResponse class. In case ex is a
        /// <c>System.Net.WebException</c>, do not use
        /// <c>createWebRequest</c>/<c>getResponse</c>/<c>HandleResponse&lt;TResponse&gt;</c> to parse the response
        /// because that will result in the same exception again. Use
        /// <c>ThrowWebServiceException&lt;YourErrorResponseType&gt;</c> to parse the response and to throw a
        /// <c>WebServiceException</c> containing the parsed DTO. Then override Send to handle that exception.
        /// </summary>
        protected virtual bool HandleResponseException<TResponse>(Exception ex, object request, string requestUri,
            Func<WebRequest> createWebRequest, Func<WebRequest, WebResponse> getResponse, out TResponse response)
        {
            var webEx = ex as WebException;
            try
            {
                var hasRefreshToken = refreshToken != null;
                if (WebRequestUtils.ShouldAuthenticate(webEx,
                    (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    || credentials != null
                    || bearerToken != null
                    || hasRefreshToken
                    || OnAuthenticationRequired != null))
                {
                    if (EnableAutoRefreshToken && hasRefreshToken)
                    {
                        var refreshRequest = new GetAccessToken {
                            RefreshToken = RefreshToken,
                        };                        
                        var uri = this.RefreshTokenUri ?? this.BaseUri.CombineWith(refreshRequest.ToPostUrl());
                        
                        this.BearerToken = null;
                        this.CookieContainer?.DeleteCookie(new Uri(BaseUri), "ss-tok");

                        GetAccessTokenResponse tokenResponse;
                        try
                        {
                            var httpReq = WebRequest.CreateHttp(uri);
                            tokenResponse = SendStringToUrl(httpReq, method:HttpMethods.Post, 
                                requestFilter: req => req.CookieContainer = CookieContainer, 
                                requestBody:refreshRequest.ToJson(), accept:MimeTypes.Json, contentType:MimeTypes.Json)
                                .FromJson<GetAccessTokenResponse>();
                        }
                        catch (WebException refreshEx)
                        {
                            var webServiceEx = ToWebServiceException(refreshEx,
                                stream => DeserializeFromStream<TResponse>(stream),
                                ContentType);

                            if (webServiceEx != null)
                                throw new RefreshTokenException(webServiceEx);
                            
                            throw new RefreshTokenException(refreshEx.Message, refreshEx);
                        }

                        var accessToken = tokenResponse?.AccessToken;
                        var refreshClient = (HttpWebRequest) createWebRequest();
                        var tokenCookie = this.GetTokenCookie();

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            refreshClient.AddBearerToken(this.BearerToken = accessToken);
                        }
                        else if (tokenCookie != null)
                        {
                            refreshClient.CookieContainer.SetTokenCookie(BaseUri, tokenCookie);
                        }
                        else throw new RefreshTokenException("Could not retrieve new AccessToken from: " + uri);

                        var refreshResponse = getResponse(refreshClient);
                        response = HandleResponse<TResponse>(refreshResponse);
                        return true;
                    }

                    OnAuthenticationRequired?.Invoke();

                    var client = createWebRequest();

                    HandleAuthException(ex, client);

                    var webResponse = getResponse(client);
                    response = HandleResponse<TResponse>(webResponse);

                    return true;
                }
            }
            catch (WebServiceException /*rethrow*/)
            {
                throw;
            }
            catch (Exception subEx)
            {
                // Since we are effectively re-executing the call, 
                // the new exception should be shown to the caller rather
                // than the old one.
                // The new exception is either this one or the one thrown
                // by the following method.
                ThrowResponseTypeException<TResponse>(request, subEx, requestUri);
                throw;
            }

            if (ExceptionFilter != null && webEx?.Response != null)
            {
                var cachedResponse = ExceptionFilter(webEx, webEx.Response, requestUri, typeof(TResponse));
                if (cachedResponse is TResponse)
                {
                    response = (TResponse)cachedResponse;
                    return true;
                }
            }

            // If this doesn't throw, the calling method 
            // should rethrow the original exception upon
            // return value of false.
            ThrowResponseTypeException<TResponse>(request, ex, requestUri);

            response = default(TResponse);
            return false;
        }

        private void HandleAuthException(Exception ex, WebRequest client)
        {
            var webEx = ex as WebException;
            if (webEx?.Response != null)
            {
                var headers = ((HttpWebResponse)webEx.Response).Headers;
                var doAuthHeader = PclExportClient.Instance.GetHeader(headers,
                    HttpHeaders.WwwAuthenticate, x => x.Contains("realm"));

                if (doAuthHeader == null)
                {
                    client.AddBasicAuth(this.UserName, this.Password);
                }
                else 
                {
                    this.authInfo = new AuthenticationInfo(doAuthHeader);
                    if (authInfo?.method == "basic" || authInfo?.method == "digest")
                    {
                        client.AddAuthInfo(this.UserName, this.Password, authInfo);
                    }
                    else if (UserName != null && Password != null)
                    {
                        client.AddBasicAuth(this.UserName, this.Password);
                    }
                }
            }
        }

        readonly ConcurrentDictionary<Type, Action<Exception, string>> ResponseHandlers = new();

        protected void ThrowResponseTypeException<TResponse>(object request, Exception ex, string requestUri)
        {
            var responseType = WebRequestUtils.GetErrorResponseDtoType<TResponse>(request);
            if (!ResponseHandlers.TryGetValue(responseType, out var responseHandler))
            {
                var mi = GetType().GetInstanceMethod("ThrowWebServiceException")
                    .MakeGenericMethod(new[] { responseType });

                responseHandler = (Action<Exception, string>)mi.CreateDelegate(
                    typeof(Action<Exception, string>), this);

                ResponseHandlers[responseType] = responseHandler;
            }
            responseHandler(ex, requestUri);
        }

        public static WebServiceException ToWebServiceException(WebException webEx, Func<Stream, object> parseDtoFn, string contentType)
        {
            if (webEx?.Response != null && webEx.Status == WebExceptionStatus.ProtocolError)
            {
                var errorResponse = (HttpWebResponse)webEx.Response;
                log.Error(webEx);
                if (log.IsDebugEnabled)
                {
                    log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
                    log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);
                }

                var serviceEx = new WebServiceException(errorResponse.StatusDescription)
                {
                    StatusCode = (int)errorResponse.StatusCode,
                    StatusDescription = errorResponse.StatusDescription,
                    ResponseHeaders = errorResponse.Headers
                };

                try
                {
                    if (string.IsNullOrEmpty(errorResponse.ContentType) || errorResponse.ContentType.MatchesContentType(contentType))
                    {
                        var ms = errorResponse.ResponseStream().CopyToNewMemoryStream();
                        serviceEx.ResponseBody = ms.ReadToEnd();
                        serviceEx.ResponseDto = parseDtoFn?.Invoke(ms);

                        if (ms.CanRead)
                            ms.Dispose(); //alt ms throws when you dispose twice
                    }
                    else
                    {
                        serviceEx.ResponseBody = errorResponse.ResponseStream().ToUtf8String();
                    }
                }
                catch (Exception innerEx)
                {
                    // Oh, well, we tried
                    return new WebServiceException(errorResponse.StatusDescription, innerEx)
                    {
                        StatusCode = (int)errorResponse.StatusCode,
                        StatusDescription = errorResponse.StatusDescription,
                        ResponseBody = serviceEx.ResponseBody
                    };
                }

                //Escape deserialize exception handling and return here
                return serviceEx;
            }

            return null;
        }

        public void ThrowWebServiceException<TResponse>(Exception ex, string requestUri)
        {
            var webEx = ToWebServiceException(
                ex as WebException, 
                stream => DeserializeFromStream<TResponse>(stream),
                ContentType);

            if (webEx != null)
                throw webEx;

            if (ex is AuthenticationException authEx)
            {
                throw WebRequestUtils.CreateCustomException(requestUri, authEx);
            }
        }

        protected virtual WebRequest SendRequest(string httpMethod, string requestUri, object request)
        {
            return PrepareWebRequest(httpMethod, requestUri, request, client => {
                using var requestStream = PclExport.Instance.GetRequestStream(client);
                SerializeRequestToStream(request, requestStream);
            });
        }
        
        protected virtual void SerializeRequestToStream(object request, Stream requestStream, bool keepOpen=false)
        {
            HttpLog?.AppendLine();
            
            if (request is string str)
            {
                requestStream.Write(str);
                HttpLog?.AppendLine(str);
            }
            else if (request is byte[] bytes)
            {
                requestStream.Write(bytes, 0, bytes.Length);
                HttpLog?.Append("(base64) ");
                HttpLog?.AppendLine(Convert.ToBase64String(bytes));
            }
            else if (request is Stream stream)
            {
                stream.WriteTo(requestStream);

                if (HttpLog != null)
                {
                    if (stream.CanSeek)
                    {
                        stream.Position = 0;
                        HttpLog.Append("(base64) ");
                        HttpLog.AppendLine(Convert.ToBase64String(stream.ReadFully()));
                    }
                    else
                    {
                        HttpLog.Append("(non-seekable stream)");
                    }
                }
            }
            else
            {
                var compressor = StreamCompressors.Get(RequestCompressionType);
                if (compressor != null)
                    requestStream = compressor.Compress(requestStream, leaveOpen: true);

                if (HttpLog == null)
                {
                    SerializeToStream(null, request, requestStream);
                }
                else
                {
                    using var ms = MemoryStreamFactory.GetStream();
                    SerializeToStream(null, request, ms);
                    ms.Position = 0;

                    if (ContentType.IsBinary())
                    {
                        HttpLog.Append("(base64) ");
                        HttpLog.AppendLine(Convert.ToBase64String(ms.ReadFully()));
                    }
                    else
                    {
                        var text = ms.ReadToEnd();
                        HttpLog.AppendLine(text);
                    }

                    ms.Position = 0;
                    ms.CopyTo(requestStream);
                }

                if (!keepOpen)
                    requestStream.Close();
            }
        }

        protected WebRequest PrepareWebRequest(string httpMethod, string requestUri, object request, Action<HttpWebRequest> sendRequestAction)
        {
            if (httpMethod == null)
                throw new ArgumentNullException(nameof(httpMethod));

            this.PopulateRequestMetadata(request);

            if (!HttpUtils.HasRequestBody(httpMethod) && request != null)
            {
                var queryString = QueryStringSerializer.SerializeToString(request);
                if (!string.IsNullOrEmpty(queryString))
                {
                    requestUri += "?" + queryString;
                }
            }

            var client = WebRequest.CreateHttp(requestUri);

            try
            {
                client.Accept = Accept;
                client.Method = httpMethod;
                PclExportClient.Instance.AddHeader(client, Headers);

                if (Proxy != null) 
                    client.Proxy = Proxy;
                
                PclExport.Instance.Config(client,
                    allowAutoRedirect: AllowAutoRedirect,
                    timeout: this.Timeout,
                    readWriteTimeout: ReadWriteTimeout,
                    userAgent: UserAgent);

                if (this.authInfo != null && !string.IsNullOrEmpty(this.UserName))
                    client.AddAuthInfo(this.UserName, this.Password, authInfo);
                else if (this.BearerToken != null)
                    client.Headers[HttpHeaders.Authorization] = "Bearer " + this.BearerToken;
                else if (this.Credentials != null)
                    client.Credentials = this.Credentials;
                else if (this.AlwaysSendBasicAuthHeader)
                    client.AddBasicAuth(this.UserName, this.Password);

                if (!DisableAutoCompression)
                {
                    PclExport.Instance.AddCompression(client);
                }

                if (StoreCookies)
                {
                    PclExportClient.Instance.SetCookieContainer(client, this);
                }

                ApplyWebRequestFilters(client);

                if (HttpUtils.HasRequestBody(httpMethod))
                {
                    client.ContentType = ContentType;

                    if (RequestCompressionType != null)
                        client.Headers[HttpHeaders.ContentEncoding] = RequestCompressionType;

                    if (HttpLog != null)
                        client.AppendHttpRequestHeaders(HttpLog, new Uri(BaseUri));

                    sendRequestAction?.Invoke(client);
                }
                else
                {
                    if (HttpLog != null)
                        client.AppendHttpRequestHeaders(HttpLog, new Uri(BaseUri));
                }
            
                HttpLog?.AppendLine();
            }
            catch (AuthenticationException ex)
            {
                throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
            }
            
            return client;
        }

        private void ApplyWebResponseFilters(WebResponse webResponse)
        {
            if (webResponse is not HttpWebResponse response) 
                return;

            ResponseFilter?.Invoke(response);
            GlobalResponseFilter?.Invoke(response);
        }

        private void ApplyWebRequestFilters(HttpWebRequest client)
        {
            RequestFilter?.Invoke(client);
            GlobalRequestFilter?.Invoke(client);
        }

        public byte[] DownloadBytes(string httpMethod, string requestUri, object request)
        {
            var webRequest = SendRequest(httpMethod, requestUri, request);
            using var response = webRequest.GetResponse();
            ApplyWebResponseFilters(response);
            using var stream = response.ResponseStream();
            return stream.ReadFully();
        }

        public async Task<byte[]> DownloadBytesAsync(string httpMethod, string requestUri, object request)
        {
            var webRequest = SendRequest(httpMethod, requestUri, request);
            using var response = await webRequest.GetResponseAsync();
            ApplyWebResponseFilters(response);
            using var stream = response.ResponseStream();
            return await stream.ReadFullyAsync();
        }

        public virtual void Publish(object requestDto)
        {
            SendOneWay(requestDto);
        }

        public void PublishAll(IEnumerable<object> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            this.PopulateRequestMetadatas(requests);
            SendOneWay(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests);
        }

        public void Publish<T>(T requestDto)
        {
            SendOneWay(requestDto);
        }

        public void Publish<T>(IMessage<T> message)
        {
            Diagnostics.ServiceStack.InitMessage(message);
            var requestDto = message.GetBody();

            if (message.CreatedDate != default(DateTime))
                Headers.Set("X-CreatedDate", message.CreatedDate.ToJsv());
            if (message.Priority != default(int))
                Headers.Set("X-Priority", message.Priority.ToString());
            if (message.RetryAttempts != default(int))
                Headers.Set("X-RetryAttempts", message.RetryAttempts.ToString());
            if (message.ReplyId != null)
                Headers.Set("X-ReplyId", message.ReplyId.Value.ToString());
            if (message.ReplyTo != null)
                Headers.Set("X-ReplyTo", message.ReplyTo);
            if (message.Tag != null)
                Headers.Set("X-Tag", message.Tag);

            SendOneWay(requestDto);
        }

        public virtual void SendOneWay(object request)
        {
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
            var httpMethod = GetHttpMethod(request) ?? HttpMethod ?? DefaultHttpMethod;
            SendOneWay(httpMethod, ResolveUrl(httpMethod, requestUri), request);
        }

        public virtual void SendOneWay(string relativeOrAbsoluteUrl, object request)
        {
            var httpMethod = GetHttpMethod(request) ?? HttpMethod ?? DefaultHttpMethod;
            SendOneWay(httpMethod, ResolveUrl(httpMethod, relativeOrAbsoluteUrl), request);
        }

        public virtual void SendAllOneWay(IEnumerable<object> requests)
        {
            PublishAll(requests);
        }

        public virtual void SendOneWay(string httpMethod, string relativeOrAbsoluteUrl, object requestDto)
        {
            var requestUri = ToAbsoluteUrl(relativeOrAbsoluteUrl);
            try
            {
                DownloadBytes(httpMethod, requestUri, requestDto);
            }
            catch (Exception ex)
            {
                if (!HandleResponseException(
                    ex,
                    requestDto,
                    requestUri,
                    () => SendRequest(httpMethod, requestUri, requestDto),
                    c => c.GetResponse(),
                    out HttpWebResponse response))
                {
                    throw;
                }

                using (response) { } //auto dispose
            }
        }

        public virtual async Task<TResponse> SendAsync<TResponse>(object request, CancellationToken token = default)
        {
            if (typeof(TResponse) == typeof(object))
                return (TResponse) await this.SendAsync(this.GetResponseType(request), request, token);

            var httpMethod = GetHttpMethod(request);
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

            return await asyncClient.SendAsync<TResponse>(httpMethod, requestUri, request, token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requests, CancellationToken token)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";
            this.PopulateRequestMetadatas(requests);
            return asyncClient.SendAsync<List<TResponse>>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests, token);
        }

        public Task PublishAsync(object request, CancellationToken token)
        {
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
            return asyncClient.SendAsync<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), request, token);
        }

        public Task PublishAllAsync(IEnumerable<object> requests, CancellationToken token)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            this.PopulateRequestMetadatas(requests);
            return asyncClient.SendAsync<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests, token);
        }


        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(httpMethod, absoluteUrl, request, token);
        }

        public virtual Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, token);
        }

        public virtual Task<TResponse> GetAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, token);
        }

        public virtual Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null, token);
        }

        public virtual Task GetAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null, token);
        }


        public virtual Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, token);
        }

        public virtual Task<TResponse> DeleteAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, token);
        }

        public virtual Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null, token);
        }

        public virtual Task DeleteAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null, token);
        }


        public virtual Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> PostAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request, token);
        }

        public virtual Task PostAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto, token);
        }


        public virtual Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> PutAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request, token);
        }

        public virtual Task PutAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto, token);
        }


        public virtual Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> PatchAsync<TResponse>(object requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> PatchAsync<TResponse>(string relativeOrAbsoluteUrl, object request, CancellationToken token = default)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, ResolveUrl(HttpMethods.Patch, relativeOrAbsoluteUrl), request, token);
        }

        public virtual Task PatchAsync(IReturnVoid requestDto, CancellationToken token = default)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto, token);
        }


        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto, CancellationToken token = default)
        {
            return CustomMethodAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto, CancellationToken token = default)
        {
            return CustomMethodAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestDto, token);
        }

        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request, CancellationToken token = default)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? request : null;
            return asyncClient.SendAsync<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody, token);
        }

        public virtual Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto, CancellationToken token = default)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return asyncClient.SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody, token);
        }

        public virtual TResponse Send<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, object request)
        {
            var requestUri = ToAbsoluteUrl(relativeOrAbsoluteUrl);

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, requestUri, request);
                if (response is TResponse typedResponse)
                    return typedResponse;
            }

            var client = SendRequest(httpMethod, requestUri, request);

            try
            {
                var webResponse = client.GetResponse();
                ApplyWebResponseFilters(webResponse);

                var response = GetResponse<TResponse>(webResponse);
                ResultsFilterResponse?.Invoke(webResponse, response, httpMethod, requestUri, request);

                DisposeIfRequired<TResponse>(webResponse);

                return response;
            }
            catch (Exception ex)
            {
                if (!HandleResponseException(
                    ex,
                    request,
                    requestUri,
                    () => SendRequest(httpMethod, requestUri, request),
                    c => c.GetResponse(),
                    out TResponse response))
                {
                    throw;
                }

                return response;
            }
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }

        public void ClearCookies()
        {
            CookieContainer = new CookieContainer();
        }

        public Dictionary<string, string> GetCookieValues()
        {
            return CookieContainer.ToDictionary(BaseUri);
        }

        public void SetCookie(string name, string value, TimeSpan? expiresIn = null)
        {
            this.SetCookie(new Uri(BaseUri), name, value,
                expiresIn != null ? DateTime.UtcNow.Add(expiresIn.Value) : (DateTime?)null);
        }

        public virtual void Get(IReturnVoid requestDto)
        {
            Send<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Get(url)) { ... }
        /// </summary>
        [Obsolete("Use: using var res = client.Get<HttpWebResponse>(requestDto)")]
        public virtual HttpWebResponse Get(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Get(url)) { ... }
        /// </summary>
        [Obsolete("Use: using var res = client.Get<HttpWebResponse>(relativeOrAbsoluteUrl)")]
        public virtual HttpWebResponse Get(string relativeOrAbsoluteUrl)
        {
            return Send<HttpWebResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);
        }

        public virtual TResponse Get<TResponse>(IReturn<TResponse> requestDto)
        {
            return Send<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        public virtual TResponse Get<TResponse>(object requestDto)
        {
            return Send<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        public virtual TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
        {
            return Send<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);
        }

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
                response = Send<QueryResponse<TResponse>>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, queryDto), null);
                foreach (var result in response.Results)
                {
                    yield return result;
                }
                query.Skip = query.Skip.GetValueOrDefault(0) + response.Results.Count;
            }
            while (response.Results.Count + response.Offset < response.Total);
        }

        public virtual void Delete(IReturnVoid requestDto)
        {
            Send<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        
        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Delete(url)) { ... }
        /// </summary>
        [Obsolete("Use: using (client.Delete<HttpWebResponse>(requestDto) { }")]
        public virtual HttpWebResponse Delete(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Delete(url)) { ... }
        /// </summary>
        [Obsolete("Use: using (client.Delete<HttpWebResponse>(relativeOrAbsoluteUrl) { }")]
        public virtual HttpWebResponse Delete(string relativeOrAbsoluteUrl)
        {
            return Send<HttpWebResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);
        }

        public virtual TResponse Delete<TResponse>(IReturn<TResponse> requestDto)
        {
            return Send<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        public virtual TResponse Delete<TResponse>(object requestDto)
        {
            return Send<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        public virtual TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
        {
            return Send<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);
        }


        public virtual void Post(IReturnVoid requestDto)
        {
            Send<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Post(url)) { ... }
        /// </summary>
        [Obsolete("Use: using (client.Post<HttpWebResponse>(requestDto) { }")]
        public virtual HttpWebResponse Post(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public virtual TResponse Post<TResponse>(IReturn<TResponse> requestDto)
        {
            return Send<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public virtual TResponse Post<TResponse>(object requestDto)
        {
            return Send<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public virtual TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return Send<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), requestDto);
        }

        public virtual void Put(IReturnVoid requestDto)
        {
            Send<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Put(url)) { ... }
        /// </summary>
        [Obsolete("Use: using (client.Put<HttpWebResponse>(requestDto) { }")]
        public virtual HttpWebResponse Put(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public virtual TResponse Put<TResponse>(IReturn<TResponse> requestDto)
        {
            return Send<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public virtual TResponse Put<TResponse>(object requestDto)
        {
            return Send<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public virtual TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return Send<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), requestDto);
        }


        public virtual void Patch(IReturnVoid requestDto)
        {
            Send<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }

        [Obsolete("Use: using (client.Patch<HttpWebResponse>(requestDto) { }")]
        public virtual HttpWebResponse Patch(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }

        public virtual TResponse Patch<TResponse>(IReturn<TResponse> requestDto)
        {
            return Send<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }

        public virtual TResponse Patch<TResponse>(object requestDto)
        {
            return Send<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }

        public virtual TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return Send<TResponse>(HttpMethods.Patch, ResolveUrl(HttpMethods.Patch, relativeOrAbsoluteUrl), requestDto);
        }


        public virtual void CustomMethod(string httpVerb, IReturnVoid requestDto)
        {
            Send<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestDto);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.CustomMethod(method,dto)) { ... }
        /// </summary>
        public virtual HttpWebResponse CustomMethod(string httpVerb, object requestDto)
        {
            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return Send<HttpWebResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.CustomMethod(method,dto)) { ... }
        /// </summary>
        public virtual HttpWebResponse CustomMethod(string httpVerb, string relativeOrAbsoluteUrl, object requestDto)
        {
            if (!HttpMethods.AllVerbs.Contains(httpVerb.ToUpper()))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            return Send<HttpWebResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestDto);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return Send<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, object requestDto)
        {
            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return CustomMethod<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object requestDto = null)
        {
            if (!HttpMethods.AllVerbs.Contains(httpVerb.ToUpper()))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            return Send<TResponse>(httpVerb, relativeOrAbsoluteUrl, requestDto);
        }

        public string GetHttpMethod(object request) => ServiceClientUtils.GetHttpMethod(request.GetType());

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Head(request)) { ... }
        /// </summary>
        public virtual HttpWebResponse Head(IReturn requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, ResolveTypedUrl(HttpMethods.Head, requestDto), requestDto);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Head(request)) { ... }
        /// </summary>
        public virtual HttpWebResponse Head(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, ResolveTypedUrl(HttpMethods.Head, requestDto), requestDto);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Head(request)) { ... }
        /// </summary>
        public virtual HttpWebResponse Head(string relativeOrAbsoluteUrl)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, ResolveUrl(HttpMethods.Head, relativeOrAbsoluteUrl), null);
        }

        public static string SendStringToUrl(HttpWebRequest webReq, string method, string requestBody, string contentType,
            string accept="*/*", Action<HttpWebRequest> requestFilter=null, Action<HttpWebResponse> responseFilter=null)
        {
            if (method != null)
                webReq.Method = method;
            if (contentType != null)
                webReq.ContentType = contentType;

            webReq.Accept = accept;
            PclExport.Instance.AddCompression(webReq);

            requestFilter?.Invoke(webReq);

            if (requestBody != null)
            {
                using var reqStream = PclExport.Instance.GetRequestStream(webReq);
                using var writer = new StreamWriter(reqStream, HttpUtils.UseEncoding);
                writer.Write(requestBody);
            }
            else if (method != null && HttpUtils.HasRequestBody(method))
            {
                webReq.ContentLength = 0;
            }

            using var webRes = webReq.GetResponse();
            using var stream = webRes.GetResponseStream();
            responseFilter?.Invoke((HttpWebResponse)webRes);
            return stream.ReadToEnd(HttpUtils.UseEncoding);
        }
        
        public static async Task<string> SendStringToUrlAsync(HttpWebRequest webReq, 
            string method, string requestBody, string contentType, string accept="*/*",
            Action<HttpWebRequest> requestFilter=null, Action<HttpWebResponse> responseFilter=null, CancellationToken token=default)
        {
            if (method != null)
                webReq.Method = method;
            if (contentType != null)
                webReq.ContentType = contentType;

            webReq.Accept = accept;
            PclExport.Instance.AddCompression(webReq);

            requestFilter?.Invoke(webReq);

            if (requestBody != null)
            {
                using var reqStream = PclExport.Instance.GetRequestStream(webReq);
                using var writer = new StreamWriter(reqStream, HttpUtils.UseEncoding);
                await writer.WriteAsync(requestBody).ConfigAwait();
            }
            else if (method != null && HttpUtils.HasRequestBody(method))
            {
                webReq.ContentLength = 0;
            }

            using var webRes = await webReq.GetResponseAsync().ConfigAwait();
            responseFilter?.Invoke((HttpWebResponse)webRes);
            using var stream = webRes.GetResponseStream();
            return await stream.ReadToEndAsync().ConfigAwait();
        }
        
        public virtual TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequest<TResponse>(ResolveTypedUrl(GetHttpMethod(request) ?? HttpMethods.Post, request), request, files.ToArray());
        }

        public virtual TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequest<TResponse>(ResolveUrl(
                GetHttpMethod(request) ?? HttpMethods.Post, relativeOrAbsoluteUrl), request, files.ToArray());
        }

        private TResponse PostFilesWithRequest<TResponse>(string requestUri, object request, UploadFile[] files)
        {
            var fileCount = 0;
            long currentStreamPosition = 0;

            WebRequest createWebRequest()
            {
                var webRequest = PrepareWebRequest(GetHttpMethod(request) ?? HttpMethods.Post, requestUri, null, null);

                var queryString = QueryStringSerializer.SerializeToString(request);

                var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);
                var boundary = Guid.NewGuid().ToString("N");
                webRequest.ContentType = "multipart/form-data; boundary=\"" + boundary + "\"";
                boundary = "--" + boundary;
                var newLine = "\r\n";
                using var outputStream = PclExport.Instance.GetRequestStream(webRequest);
                foreach (var key in nameValueCollection.AllKeys)
                {
                    outputStream.Write(boundary + newLine);
                    outputStream.Write($"Content-Disposition: form-data;name=\"{key}\"{newLine}");
                    outputStream.Write($"Content-Type: text/plain;charset=utf-8{newLine}{newLine}");
                    outputStream.Write(nameValueCollection[key] + newLine);
                }

                var buffer = new byte[4096];
                for (fileCount = 0; fileCount < files.Length; fileCount++)
                {
                    var file = files[fileCount];
                    currentStreamPosition = file.Stream.Position;
                    outputStream.Write(boundary + newLine);
                    var fileName = file.FileName ?? $"upload{fileCount}";
                    var fieldName = file.FieldName ?? $"upload{fileCount}";
                    var contentType = file.ContentType ?? (file.FileName != null ? MimeTypes.GetMimeType(file.FileName) : null) ?? "application/octet-stream";
                    outputStream.Write($"Content-Disposition: form-data;name=\"{fieldName}\";filename=\"{fileName}\"{newLine}Content-Type: {contentType}{newLine}{newLine}");

                    int byteCount;
                    int bytesWritten = 0;
                    while ((byteCount = file.Stream.Read(buffer, 0, 4096)) > 0)
                    {
                        outputStream.Write(buffer, 0, byteCount);

                        if (OnUploadProgress != null)
                        {
                            bytesWritten += byteCount;
                            OnUploadProgress(bytesWritten, file.Stream.Length);
                        }
                    }

                    outputStream.Write(newLine);
                    if (fileCount == files.Length - 1) 
                        outputStream.Write(boundary + "--");
                }

                return webRequest;
            }

            try
            {
                var webRequest = createWebRequest();
                var webResponse = webRequest.GetResponse();
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                // restore original position before retry
                files.Last().Stream.Seek(currentStreamPosition, SeekOrigin.Begin);

                if (!HandleResponseException(
                    ex, request, requestUri, createWebRequest,
                    c => PclExport.Instance.GetResponse(c),
                    out TResponse response))
                {
                    throw;
                }

                return response;
            }
        }

        public virtual TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "file")
        {
            return PostFileWithRequest<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), fileToUpload, fileName, request, fieldName);
        }

        public virtual TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "file")
        {
            var requestUri = ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl);
            var currentStreamPosition = fileToUpload.Position;

            WebRequest createWebRequest()
            {
                var webRequest = PrepareWebRequest(HttpMethods.Post, requestUri, null, null);

                var queryString = QueryStringSerializer.SerializeToString(request);

                var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);
                var boundary = "----------------------------" + Guid.NewGuid().ToString("N");
                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                boundary = "--" + boundary;
                var newLine = "\r\n";
                using var outputStream = PclExport.Instance.GetRequestStream(webRequest);
                foreach (var key in nameValueCollection.AllKeys)
                {
                    outputStream.Write(boundary + newLine);
                    outputStream.Write($"Content-Disposition: form-data;name=\"{key}\"{newLine}");
                    outputStream.Write($"Content-Type: text/plain;charset=utf-8{newLine}{newLine}");
                    outputStream.Write(nameValueCollection[key] + newLine);
                }

                outputStream.Write(boundary + newLine);
                outputStream.Write($"Content-Disposition: form-data;name=\"{fieldName}\";filename=\"{fileName}\"{newLine}{newLine}");
                var buffer = new byte[4096];
                int byteCount;
                int bytesWritten = 0;
                while ((byteCount = fileToUpload.Read(buffer, 0, 4096)) > 0)
                {
                    outputStream.Write(buffer, 0, byteCount);

                    if (OnUploadProgress != null)
                    {
                        bytesWritten += byteCount;
                        OnUploadProgress(bytesWritten, fileToUpload.Length);
                    }
                }
                outputStream.Write(newLine);
                outputStream.Write(boundary + "--");

                return webRequest;
            }

            try
            {
                var webRequest = createWebRequest();
                var webResponse = PclExport.Instance.GetResponse(webRequest);
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                // restore original position before retry
                fileToUpload.Seek(currentStreamPosition, SeekOrigin.Begin);

                if (!HandleResponseException(
                    ex, request, requestUri, createWebRequest,
                    c => PclExport.Instance.GetResponse(c),
                    out TResponse response))
                {
                    throw;
                }

                return response;
            }
        }
        
        private static byte[] GetHeaderBytes(string fileName, string mimeType, string field, string boundary)
        {
            var header = "\r\n--" + boundary +
                         $"\r\nContent-Disposition: form-data; name=\"{field}\"; filename=\"{fileName}\"\r\nContent-Type: {mimeType}\r\n\r\n";

            var headerBytes = header.ToAsciiBytes();
            return headerBytes;
        }

        public static void UploadFile(WebRequest webRequest, Stream fileStream, string fileName, string mimeType,
            string accept = null, Action<HttpWebRequest> requestFilter = null, string method = "POST",
            string fieldName = "file")
        {
            var httpReq = (HttpWebRequest)webRequest;
            httpReq.Method = method;

            if (accept != null)
                httpReq.Accept = accept;

            requestFilter?.Invoke(httpReq);

            var boundary = Guid.NewGuid().ToString("N");

            httpReq.ContentType = "multipart/form-data; boundary=\"" + boundary + "\"";

            var boundaryBytes = ("\r\n--" + boundary + "--\r\n").ToAsciiBytes();

            var headerBytes = GetHeaderBytes(fileName, mimeType, fieldName, boundary);

            var contentLength = fileStream.Length + headerBytes.Length + boundaryBytes.Length;
            PclExport.Instance.InitHttpWebRequest(httpReq,
                contentLength: contentLength, allowAutoRedirect: false, keepAlive: false);

            using var outputStream = PclExport.Instance.GetRequestStream(httpReq);
            outputStream.Write(headerBytes, 0, headerBytes.Length);
            fileStream.CopyTo(outputStream, 4096);
            outputStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            PclExport.Instance.CloseStream(outputStream);
        }

        public virtual TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType, string fieldName = "file")
        {
            var currentStreamPosition = fileToUpload.Position;
            var requestUri = ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl);
            WebRequest createWebRequest() => PrepareWebRequest(HttpMethods.Post, requestUri, null, null);

            try
            {
                var webRequest = createWebRequest();
                UploadFile(webRequest, fileToUpload, fileName:fileName, mimeType:mimeType, fieldName:fieldName);
                var webResponse = PclExport.Instance.GetResponse(webRequest);
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                // restore original position before retry
                fileToUpload.Seek(currentStreamPosition, SeekOrigin.Begin);

                if (!HandleResponseException(ex,
                    null,
                    requestUri,
                    createWebRequest,
                    c =>
                    {
                        UploadFile(c, fileToUpload, fileName:fileName, mimeType:mimeType, fieldName:fieldName);
                        return PclExport.Instance.GetResponse(c);
                    },
                    out TResponse response))
                {
                    throw;
                }

                return response;
            }
        }

        private TResponse HandleResponse<TResponse>(WebResponse webResponse)
        {
            ApplyWebResponseFilters(webResponse);

            var response = GetResponse<TResponse>(webResponse);
            DisposeIfRequired<TResponse>(webResponse);

            return response;
        }

        private void DisposeIfRequired<TResponse>(WebResponse webResponse)
        {
            if (typeof(TResponse) == typeof(HttpWebResponse) && webResponse is HttpWebResponse)
                return;
            if (typeof(TResponse) == typeof(Stream))
                return;

            if (HttpLog != null)
                HttpLogFilter?.Invoke(HttpLog);
            
            using (webResponse) { }
        }

        protected TResponse GetResponse<TResponse>(WebResponse webRes)
        {
            //Callee Needs to dispose of response manually
            if (typeof(TResponse) == typeof(HttpWebResponse) && webRes is HttpWebResponse)
            {
                return (TResponse)Convert.ChangeType(webRes, typeof(TResponse), null);
            }
            if (typeof(TResponse) == typeof(Stream))
            {
                return (TResponse)(object)webRes.ResponseStream();
            }

            using var responseStream = webRes.ResponseStream();
            var stream = responseStream;
            
            if (HttpLog != null)
            {
                stream = new MemoryStream();
                responseStream.CopyTo(stream);
                stream.Position = 0;
                
                ((HttpWebResponse)webRes).AppendHttpResponseHeaders(HttpLog);
                if (webRes.ContentLength != 0 && ((HttpWebResponse) webRes).StatusCode != HttpStatusCode.NoContent)
                {
                    var isBinary = typeof(TResponse) == typeof(Stream) || typeof(TResponse) == typeof(byte[]) || ContentType.IsBinary();
                    if (isBinary)
                    {
                    
                        HttpLog.Append("(base64) ");
                        HttpLog.AppendLine(Convert.ToBase64String(stream.ReadFully()));
                    }
                    else
                    {
                        HttpLog.AppendLine(stream.ReadToEnd());
                    }
                }
                HttpLog.AppendLine().AppendLine();
                stream.Position = 0;
            }
            
            if (typeof(TResponse) == typeof(string))
            {
                return (TResponse)(object)stream.ReadToEnd();
            }
            if (typeof(TResponse) == typeof(byte[]))
            {
                return (TResponse)(object)stream.ReadFully();
            }

            var response = DeserializeFromStream<TResponse>(stream);
            return response;
        }

        public void Dispose() { }
    }

    public static partial class ServiceClientExtensions
    {
        public static Stream ResponseStream(this WebResponse webRes)
        {
#if !NETFRAMEWORK
            return webRes.GetResponseStream().Decompress(webRes.Headers[HttpHeaders.ContentEncoding]);
#else
            return webRes.GetResponseStream();
#endif
        }

        public static TResponse PostFile<TResponse>(this IRestClient client,
            string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType, string fieldName = "file")
        {
            using var fileStream = fileToUpload.OpenRead();
            return client.PostFile<TResponse>(relativeOrAbsoluteUrl, fileStream, 
                fileName:fileToUpload.Name, mimeType:mimeType, fieldName:fieldName);
        }

        public static TResponse PostFileWithRequest<TResponse>(this IRestClient client,
            FileInfo fileToUpload, object request, string fieldName = "file")
        {
            return client.PostFileWithRequest<TResponse>(request.ToPostUrl(), fileToUpload, 
                request:request, fieldName:fieldName);
        }

        public static TResponse PostFileWithRequest<TResponse>(this IRestClient client,
            string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request, string fieldName = "file")
        {
            using var fileStream = fileToUpload.OpenRead();
            return client.PostFileWithRequest<TResponse>(relativeOrAbsoluteUrl, fileStream, 
                fileName:fileToUpload.Name, request:request, fieldName:fieldName);
        }

        public static void PopulateRequestMetadatas(this IHasSessionId client, IEnumerable<object> requests)
        {
            foreach (var request in requests)
            {
                client.PopulateRequestMetadata(request);
            }
        }
        
        public static void PopulateRequestMetadata(this IHasSessionId client, object request)
        {
            if (client.SessionId != null)
            {
                if (request is IHasSessionId hasSession && hasSession.SessionId == null)
                    hasSession.SessionId = client.SessionId;
            }
            if (request is IHasBearerToken { BearerToken: null } hasBearer)
            {
                if (client is IHasBearerToken { BearerToken: { } } clientBearer)
                {
                    hasBearer.BearerToken = clientBearer.BearerToken;
                }
                else if (client is IServiceClient serviceClient)
                {
                    hasBearer.BearerToken = serviceClient.GetTokenCookie();
                }
            }
            if (client is IHasVersion { Version: > 0 } clientVersion)
            {
                if (request is IHasVersion { Version: <= 0 } hasVersion)
                    hasVersion.Version = clientVersion.Version;
            }
        }

        public static Dictionary<string, string> ToDictionary(this CookieContainer cookies, string baseUri)
        {
            var to = new Dictionary<string, string>();
            if (cookies == null || baseUri == null)
                return to;

            foreach (Cookie cookie in cookies.GetCookies(new Uri(baseUri)))
            {
                to[cookie.Name] = cookie.Value;
            }
            return to;
        }

        public static void SetCookie(this IServiceClient client, Uri baseUri, string name, string value,
            DateTime? expiresAt = null, string path = "/",
            bool? httpOnly = null, bool? secure = null)
        {
            AssertCookieContainer(client).SetCookie(baseUri, name, value, expiresAt, path, httpOnly, secure);
        }

        public static CookieContainer AssertCookieContainer(this IServiceClient client)
        {
            if (client is not IHasCookieContainer hasCookies)
                throw new NotSupportedException("Client does not implement IHasCookieContainer");
            return hasCookies.CookieContainer;
        }

        public static void DeleteCookie(this CookieContainer cookieContainer, Uri uri, string name)
        {
            var cookies = cookieContainer.GetCookies(uri);
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name != name) continue;
                cookie.Expired = true;
                return;
            }
        }

        public static void DeleteCookie(this IHasCookieContainer hasCookieContainer, Uri uri, string name) =>
            hasCookieContainer.CookieContainer.DeleteCookie(uri, name);

        public static void DeleteCookie(this IJsonServiceClient client, string name) =>
            client.AssertCookieContainer().DeleteCookie(new Uri(client.BaseUri), name);

        public static TResponse PostBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, object requestBody) =>
            client.Send<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PostBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, string requestBody) =>
            client.Send<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PostBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, byte[] requestBody) =>
            client.Send<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PostBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, Stream requestBody) =>
            client.Send<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);

        public static TResponse PutBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, object requestBody) =>
            client.Send<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PutBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, string requestBody) =>
            client.Send<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PutBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, byte[] requestBody) =>
            client.Send<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PutBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, Stream requestBody) =>
            client.Send<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);

        public static TResponse PatchBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, object requestBody) =>
            client.Send<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PatchBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, string requestBody) =>
            client.Send<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PatchBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, byte[] requestBody) =>
            client.Send<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);
        public static TResponse PatchBody<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, Stream requestBody) =>
            client.Send<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody);

        public static Task<TResponse> PostBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, object requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PostBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, string requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PostBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, byte[] requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PostBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, Stream requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Post, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);

        public static Task<TResponse> PutBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, object requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PutBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, string requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PutBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, byte[] requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PutBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, Stream requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Put, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);

        public static Task<TResponse> PatchBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, object requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PatchBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, string requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PatchBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, byte[] requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);
        public static Task<TResponse> PatchBodyAsync<TResponse>(this IServiceClient client, IReturn<TResponse> toRequest, Stream requestBody, CancellationToken token = default(CancellationToken)) =>
            client.SendAsync<TResponse>(HttpMethods.Patch, ((IServiceClientMeta)client).ResolveTypedUrl(HttpMethods.Get, toRequest), requestBody, token);

        public static T WithBasePath<T>(this T client, string basePath)
            where T : ServiceClientBase
        {
            client.UseBasePath = basePath;
            return client;
        }

        public static void SetCookie(this CookieContainer cookieContainer, 
            Uri baseUri, string name, string value, DateTime? expiresAt,
            string path = "/", bool? httpOnly = null, bool? secure = null)
        {
            var cookie = new Cookie(name, value, path);
            if (expiresAt != null)
                cookie.Expires = expiresAt.Value;
            if (path != null)
                cookie.Path = path;
            if (httpOnly != null)
                cookie.HttpOnly = httpOnly.Value;
            if (secure != null)
                cookie.Secure = secure.Value;

            cookieContainer.Add(baseUri, cookie);
        }

        public static string GetSessionId(this IServiceClient client)
        {
            client.GetCookieValues().TryGetValue("ss-id", out var sessionId);
            return sessionId;
        }

        public static string GetPermanentSessionId(this IServiceClient client)
        {
            client.GetCookieValues().TryGetValue("ss-pid", out var sessionId);
            return sessionId;
        }

        public static string GetOptions(this IServiceClient client)
        {
            client.GetCookieValues().TryGetValue("ss-opt", out var sessionId);
            return sessionId;
        }

        public static void SetSessionId(this IServiceClient client, string sessionId)
        {
            if (sessionId == null)
                return;

            client.SetCookie("ss-id", sessionId);
        }

        public static void SetPermanentSessionId(this IServiceClient client, string sessionId)
        {
            if (sessionId == null)
                return;

            client.SetCookie("ss-pid", sessionId, expiresIn: TimeSpan.FromDays(365 * 20));
        }

        public static void SetOptions(this IServiceClient client, string options)
        {
            if (options == null)
                return;

            client.SetCookie("ss-opt", options);
        }

        public static string GetTokenCookie(this IServiceClient client)
        {
            client.GetCookieValues().TryGetValue("ss-tok", out var token);
            return token;
        }

        public static string GetRefreshTokenCookie(this IServiceClient client)
        {
            client.GetCookieValues().TryGetValue("ss-reftok", out var token);
            return token;
        }

        public static string GetTokenCookie(this CookieContainer cookies, string baseUri)
        {
            cookies.ToDictionary(baseUri).TryGetValue("ss-tok", out var token);
            return token;
        }

        public static string GetRefreshTokenCookie(this CookieContainer cookies, string baseUri)
        {
            cookies.ToDictionary(baseUri).TryGetValue("ss-reftok", out var token);
            return token;
        }

        public static void SetTokenCookie(this IServiceClient client, string token)
        {
            if (token == null)
                return;

            client.SetCookie("ss-tok", token, expiresIn: TimeSpan.FromDays(365 * 20));
        }

        public static void DeleteTokenCookie(this IJsonServiceClient client) => client.DeleteCookie("ss-tok");

        public static void SetRefreshTokenCookie(this IServiceClient client, string token)
        {
            if (token == null)
                return;

            client.SetCookie("ss-reftok", token, expiresIn: TimeSpan.FromDays(365 * 20));
        }

        public static void DeleteRefreshTokenCookie(this IJsonServiceClient client) => client.DeleteCookie("ss-reftok");

        public static void DeleteTokenCookies(this IJsonServiceClient client)
        {
            client.DeleteTokenCookie();
            client.DeleteRefreshTokenCookie();
        }

        public static void SetTokenCookie(this CookieContainer cookies, string baseUri, string token)
        {
            if (token == null)
                return;

            cookies.SetCookie(new Uri(baseUri), "ss-tok", token,
                expiresAt: DateTime.UtcNow.Add(TimeSpan.FromDays(365 * 20)));
        }

        public static void SetRefreshTokenCookie(this CookieContainer cookies, string baseUri, string token)
        {
            if (token == null)
                return;

            cookies.SetCookie(new Uri(baseUri), "ss-reftok", token,
                expiresAt: DateTime.UtcNow.Add(TimeSpan.FromDays(365 * 20)));
        }

        public static string GetCookieValue(this AsyncServiceClient client, string name) =>
            client.GetCookieValues().TryGetValue(name, out var token) ? token : null;

        public static string GetTokenCookie(this AsyncServiceClient client) =>
            client.GetCookieValues().TryGetValue("ss-tok", out var token) ? token : null;

        public static string GetRefreshTokenCookie(this AsyncServiceClient client) =>
            client.GetCookieValues().TryGetValue("ss-reftok", out var token) ? token : null;

        public static void SetUserAgent(this HttpWebRequest req, string userAgent)
        {
            PclExport.Instance.SetUserAgent(req, userAgent);
        }

        public static void AddAuthSecret(this IRestClient client, string authsecret)
        {
            client.AddHeader(HttpHeaders.XParamOverridePrefix + nameof(authsecret), authsecret);
        }

        public static T Apply<T>(this T client, Action<T> fn)
            where T : IServiceGateway
        {
            fn(client);
            return client;
        }
    }

    public interface IHasCookieContainer
    {
        CookieContainer CookieContainer { get; }
    }

    public interface IServiceClientMeta
    {
        string Format { get; }
        string BaseUri { get; set; }
        string SyncReplyBaseUri { get; }
        string AsyncOneWayBaseUri { get; }

        int Version { get; }
        string SessionId { get; }

        string UserName { get; }
        string Password { get; }
        bool AlwaysSendBasicAuthHeader { get; }

        string BearerToken { get; set; }
        string RefreshToken { get; set; }
        string RefreshTokenUri { get; set; }

        string ResolveTypedUrl(string httpMethod, object requestDto);
        string ResolveUrl(string httpMethod, string relativeOrAbsoluteUrl);
    }

    public delegate string UrlResolverDelegate(IServiceClientMeta client, string httpMethod, string relativeOrAbsoluteUrl);
    public delegate string TypedUrlResolverDelegate(IServiceClientMeta client, string httpMethod, object requestDto);

    public delegate object ResultsFilterDelegate(Type responseType, string httpMethod, string requestUri, object request);

    public delegate void ResultsFilterResponseDelegate(WebResponse webResponse, object response, string httpMethod, string requestUri, object request);

    public delegate object ExceptionFilterDelegate(WebException webEx, WebResponse webResponse, string requestUri, Type responseType);
}
