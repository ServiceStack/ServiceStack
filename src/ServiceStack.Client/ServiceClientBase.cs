// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
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
        public static string DefaultUserAgent = "ServiceStack .NET Client " + Env.ServiceStackVersion;

        readonly AsyncServiceClient asyncClient;

        protected ServiceClientBase()
        {
            this.HttpMethod = DefaultHttpMethod;
            this.Headers = new NameValueCollection();

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
            };
            this.CookieContainer = new CookieContainer();
            this.StoreCookies = true; //leave
            this.UserAgent = DefaultUserAgent;

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
            this.BaseUri = baseUri;
            this.asyncClient.BaseUri = baseUri;
            this.SyncReplyBaseUri = baseUri.WithTrailingSlash() + Format + "/reply/";
            this.AsyncOneWayBaseUri = baseUri.WithTrailingSlash() + Format + "/oneway/";
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

        public string RequestCompressionType { get; set; }

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
        
        public string SessionId { get; set; }

        public string UserAgent
        {
            get => userAgent;
            set
            {
                userAgent = value;
                asyncClient.UserAgent = value;
            }
        }
        private string userAgent;

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

#if !SL5
        public IWebProxy Proxy { get; set; }
#endif

        private ICredentials credentials;

        /// <summary>
        /// Gets or sets authentication information for the request.
        /// Warning: It's recommened to use <see cref="UserName"/> and <see cref="Password"/> for basic auth.
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

        public bool AllowAutoRedirect
        {
            get => allowAutoRedirect;
            set => allowAutoRedirect = value;
        }
        private bool allowAutoRedirect = true;

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

        internal void AsyncSerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            SerializeRequestToStream(request, stream);
        }

        public abstract void SerializeToStream(IRequest requestContext, object request, Stream stream);

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

            if (request is IVerb)
            {
                if (request is IGet)
                    return Get<TResponse>(request);
                if (request is IPost)
                    return Post<TResponse>(request);
                if (request is IPut)
                    return Put<TResponse>(request);
                if (request is IDelete)
                    return Delete<TResponse>(request);
                if (request is IPatch)
                    return Patch<TResponse>(request);
            }

            var httpMethod = HttpMethod ?? DefaultHttpMethod;
            var requestUri = ResolveUrl(httpMethod, UrlResolver == null
                ? this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name
                : Format + "/reply/" + request.GetType().Name);

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, requestUri, request);
                if (response is TResponse)
                    return (TResponse)response;
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
                if (WebRequestUtils.ShouldAuthenticate(webEx,
                    (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    || credentials != null
                    || bearerToken != null
                    || refreshToken != null
                    || OnAuthenticationRequired != null))
                {
                    if (RefreshToken != null)
                    {
                        var refreshRequest = new GetAccessToken { RefreshToken = RefreshToken };
                        var uri = this.RefreshTokenUri ?? this.BaseUri.CombineWith(refreshRequest.ToPostUrl());

                        GetAccessTokenResponse tokenResponse;
                        try
                        {
                            tokenResponse = uri.PostJsonToUrl(refreshRequest).FromJson<GetAccessTokenResponse>();
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
                        if (string.IsNullOrEmpty(accessToken))
                            throw new RefreshTokenException("Could not retrieve new AccessToken from: " + uri);

                        var refreshClient = (HttpWebRequest) createWebRequest();
                        if (this.GetTokenCookie() != null)
                        {
                            this.SetTokenCookie(accessToken);
                            refreshClient.CookieContainer.SetTokenCookie(BaseUri, accessToken);
                        }
                        else
                        {
                            refreshClient.AddBearerToken(this.BearerToken = accessToken);
                        }

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
            catch (WebServiceException /*retrhow*/)
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

        readonly ConcurrentDictionary<Type, Action<Exception, string>> ResponseHandlers
            = new ConcurrentDictionary<Type, Action<Exception, string>>();

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
                        var bytes = errorResponse.GetResponseStream().ReadFully();
                        var stream = MemoryStreamFactory.GetStream(bytes);
                        serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                        serviceEx.ResponseDto = parseDtoFn?.Invoke(stream);

                        if (stream.CanRead)
                            stream.Dispose(); //alt ms throws when you dispose twice
                    }
                    else
                    {
                        serviceEx.ResponseBody = errorResponse.GetResponseStream().ToUtf8String();
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
            return PrepareWebRequest(httpMethod, requestUri, request, client =>
            {
                using (var requestStream = PclExport.Instance.GetRequestStream(client))
                {
                    SerializeRequestToStream(request, requestStream);
                }
            });
        }
        
        protected virtual void SerializeRequestToStream(object request, Stream requestStream, bool keepOpen=false)
        {
            var str = request as string;
            var bytes = request as byte[];
            var stream = request as Stream;
            if (str != null)
            {
                requestStream.Write(str);
            }
            else if (bytes != null)
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }
            else if (stream != null)
            {
                stream.WriteTo(requestStream);
            }
            else
            {
                if (RequestCompressionType == CompressionTypes.Deflate)
                {
                    requestStream = new System.IO.Compression.DeflateStream(requestStream, System.IO.Compression.CompressionMode.Compress);
                }
                else if (RequestCompressionType == CompressionTypes.GZip)
                {
                    requestStream = new System.IO.Compression.GZipStream(requestStream, System.IO.Compression.CompressionMode.Compress);
                }
                SerializeToStream(null, request, requestStream);

                if (!keepOpen)
                {
                    requestStream.Close();
                }
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

            var client = PclExport.Instance.CreateWebRequest(requestUri,
                emulateHttpViaPost: EmulateHttpViaPost);

            try
            {
                client.Accept = Accept;
                client.Method = httpMethod;
                PclExportClient.Instance.AddHeader(client, Headers);

                if (Proxy != null) client.Proxy = Proxy;
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

                    sendRequestAction?.Invoke(client);
                }
            }
            catch (AuthenticationException ex)
            {
                throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
            }
            return client;
        }

        private void ApplyWebResponseFilters(WebResponse webResponse)
        {
            if (!(webResponse is HttpWebResponse)) return;

            ResponseFilter?.Invoke((HttpWebResponse)webResponse);
            GlobalResponseFilter?.Invoke((HttpWebResponse)webResponse);
        }

        private void ApplyWebRequestFilters(HttpWebRequest client)
        {
            RequestFilter?.Invoke(client);
            GlobalRequestFilter?.Invoke(client);
        }

        private byte[] DownloadBytes(string httpMethod, string requestUri, object request)
        {
            var webRequest = SendRequest(httpMethod, requestUri, request);
            using (var response = webRequest.GetResponse())
            {
                ApplyWebResponseFilters(response);
                using (var stream = response.GetResponseStream())
                    return stream.ReadFully();
            }
        }

        public virtual void Publish(object requestDto)
        {
            SendOneWay(requestDto);
        }

        public void PublishAll(IEnumerable<object> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            SendOneWay(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests);
        }

        public void Publish<T>(T requestDto)
        {
            SendOneWay(requestDto);
        }

        public void Publish<T>(IMessage<T> message)
        {
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

        public static string GetExplicitMethod(object request)
        {
            if (!(request is IVerb))
                return null;

            return request is IGet ?
                  HttpMethods.Get
                : request is IPost ?
                  HttpMethods.Post
                : request is IPut ?
                  HttpMethods.Put
                : request is IDelete ?
                  HttpMethods.Delete
                : request is IPatch ?
                  HttpMethods.Patch :
                  null;
        }

        public virtual void SendOneWay(object request)
        {
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
            var httpMethod = GetExplicitMethod(request) ?? HttpMethod ?? DefaultHttpMethod;
            SendOneWay(httpMethod, ResolveUrl(httpMethod, requestUri), request);
        }

        public virtual void SendOneWay(string relativeOrAbsoluteUrl, object request)
        {
            var httpMethod = GetExplicitMethod(request) ?? HttpMethod ?? DefaultHttpMethod;
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

            var httpMethod = HttpMethod ?? DefaultHttpMethod;
            var requestUri = ResolveUrl(httpMethod, UrlResolver == null
                 ? this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name
                 : Format + "/reply/" + request.GetType().Name);

            return asyncClient.SendAsync<TResponse>(httpMethod, requestUri, request, token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requests, CancellationToken token)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";
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
            return asyncClient.SendAsync<byte[]>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, requestUri), requests, token);
        }

        public virtual Task<TResponse> SendAsync<TResponse>(object request)
        {
            return SendAsync<TResponse>(request, default(CancellationToken));
        }


        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default(CancellationToken))
        {
            return asyncClient.SendAsync<TResponse>(httpMethod, absoluteUrl, request, token);
        }

        public virtual Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        public virtual Task<TResponse> GetAsync<TResponse>(object requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        public virtual Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, ResolveUrl(HttpMethods.Get, relativeOrAbsoluteUrl), null);
        }

        public virtual Task GetAsync(IReturnVoid requestDto)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }


        public virtual Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        public virtual Task<TResponse> DeleteAsync<TResponse>(object requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

        public virtual Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, ResolveUrl(HttpMethods.Delete, relativeOrAbsoluteUrl), null);
        }

        public virtual Task DeleteAsync(IReturnVoid requestDto)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }


        public virtual Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public virtual Task<TResponse> PostAsync<TResponse>(object requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }

        public virtual Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request);
        }

        public virtual Task PostAsync(IReturnVoid requestDto)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Post, ResolveTypedUrl(HttpMethods.Post, requestDto), requestDto);
        }


        public virtual Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public virtual Task<TResponse> PutAsync<TResponse>(object requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }

        public virtual Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, ResolveUrl(HttpMethods.Put, relativeOrAbsoluteUrl), request);
        }

        public virtual Task PutAsync(IReturnVoid requestDto)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Put, ResolveTypedUrl(HttpMethods.Put, requestDto), requestDto);
        }


        public virtual Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }

        public virtual Task<TResponse> PatchAsync<TResponse>(object requestDto)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }

        public virtual Task<TResponse> PatchAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, ResolveUrl(HttpMethods.Patch, relativeOrAbsoluteUrl), request);
        }

        public virtual Task PatchAsync(IReturnVoid requestDto)
        {
            return asyncClient.SendAsync<byte[]>(HttpMethods.Patch, ResolveTypedUrl(HttpMethods.Patch, requestDto), requestDto);
        }


        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            return CustomMethodAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestDto);
        }

        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
        {
            return CustomMethodAsync<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestDto);
        }

        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? request : null;
            return asyncClient.SendAsync<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody);
        }

        public virtual Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            if (!HttpMethods.Exists(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return asyncClient.SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
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
        public virtual HttpWebResponse Get(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Get, ResolveTypedUrl(HttpMethods.Get, requestDto), null);
        }

        /// <summary>
        /// APIs returning HttpWebResponse must be explicitly Disposed, e.g using (var res = client.Get(url)) { ... }
        /// </summary>
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

        public virtual HttpWebResponse Delete(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Delete, ResolveTypedUrl(HttpMethods.Delete, requestDto), null);
        }

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

        public virtual HttpWebResponse CustomMethod(string httpVerb, object requestDto)
        {
            var requestBody = HttpUtils.HasRequestBody(httpVerb) ? requestDto : null;
            return Send<HttpWebResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

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


        public virtual HttpWebResponse Head(IReturn requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, ResolveTypedUrl(HttpMethods.Head, requestDto), requestDto);
        }

        public virtual HttpWebResponse Head(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, ResolveTypedUrl(HttpMethods.Head, requestDto), requestDto);
        }

        public virtual HttpWebResponse Head(string relativeOrAbsoluteUrl)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, ResolveUrl(HttpMethods.Head, relativeOrAbsoluteUrl), null);
        }

        public virtual TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequest<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), request, files.ToArray());
        }

        public virtual TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files)
        {
            return PostFilesWithRequest<TResponse>(ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl), request, files.ToArray());
        }

        private TResponse PostFilesWithRequest<TResponse>(string requestUri, object request, UploadFile[] files)
        {
            var fileCount = 0;
            long currentStreamPosition = 0;
            Func<WebRequest> createWebRequest = () =>
            {
                var webRequest = PrepareWebRequest(HttpMethods.Post, requestUri, null, null);

                var queryString = QueryStringSerializer.SerializeToString(request);

                var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);
                var boundary = Guid.NewGuid().ToString("N");
                webRequest.ContentType = "multipart/form-data; boundary=\"" + boundary + "\"";
                boundary = "--" + boundary;
                var newLine = "\r\n";
                using (var outputStream = PclExport.Instance.GetRequestStream(webRequest))
                {
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
                        outputStream.Write($"Content-Disposition: form-data;name=\"{fieldName}\";filename=\"{fileName}\"{newLine}Content-Type: application/octet-stream{newLine}{newLine}");

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
                }

                return webRequest;
            };

            try
            {
                var webRequest = createWebRequest();
                var webResponse = webRequest.GetResponse();
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                // restore original position before retry
                files[fileCount - 1].Stream.Seek(currentStreamPosition, SeekOrigin.Begin);

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

        public virtual TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            return PostFileWithRequest<TResponse>(ResolveTypedUrl(HttpMethods.Post, request), fileToUpload, fileName, request, fieldName);
        }

        public virtual TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "upload")
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
                using (var outputStream = PclExport.Instance.GetRequestStream(webRequest))
                {
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
                }

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


        public virtual TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
        {
            var currentStreamPosition = fileToUpload.Position;
            var requestUri = ResolveUrl(HttpMethods.Post, relativeOrAbsoluteUrl);
            WebRequest createWebRequest() => PrepareWebRequest(HttpMethods.Post, requestUri, null, null);

            try
            {
                var webRequest = createWebRequest();
                webRequest.UploadFile(fileToUpload, fileName, mimeType);
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
                        c.UploadFile(fileToUpload, fileName, mimeType);
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

        private static void DisposeIfRequired<TResponse>(WebResponse webResponse)
        {
            if (typeof(TResponse) == typeof(HttpWebResponse) && webResponse is HttpWebResponse)
                return;
            if (typeof(TResponse) == typeof(Stream))
                return;

            using (webResponse) { }
        }

        protected TResponse GetResponse<TResponse>(WebResponse webResponse)
        {
#if NETSTANDARD2_0
            var compressionType = webResponse.Headers[HttpHeaders.ContentEncoding];
#endif

            //Callee Needs to dispose of response manually
            if (typeof(TResponse) == typeof(HttpWebResponse) && webResponse is HttpWebResponse)
            {
                return (TResponse)Convert.ChangeType(webResponse, typeof(TResponse), null);
            }
            if (typeof(TResponse) == typeof(Stream))
            {
#if NETSTANDARD2_0
                return (TResponse)(object)webResponse.GetResponseStream().Decompress(compressionType);
#else
                return (TResponse)(object)webResponse.GetResponseStream();
#endif
            }

#if NETSTANDARD2_0
            using (var responseStream = webResponse.GetResponseStream().Decompress(compressionType))
#else
            using (var responseStream = webResponse.GetResponseStream())
#endif
            {
                if (typeof(TResponse) == typeof(string))
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        return (TResponse)(object)reader.ReadToEnd();
                    }
                }
                if (typeof(TResponse) == typeof(byte[]))
                {
                    return (TResponse)(object)responseStream.ReadFully();
                }

                var response = DeserializeFromStream<TResponse>(responseStream);
                return response;
            }
        }

        public void Dispose() { }
    }

    public static partial class ServiceClientExtensions
    {
        public static TResponse PostFile<TResponse>(this IRestClient client,
            string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
        {
            using (FileStream fileStream = fileToUpload.OpenRead())
            {
                return client.PostFile<TResponse>(relativeOrAbsoluteUrl, fileStream, fileToUpload.Name, mimeType);
            }
        }

        public static TResponse PostFileWithRequest<TResponse>(this IRestClient client,
            FileInfo fileToUpload, object request, string fieldName = "upload")
        {
            return client.PostFileWithRequest<TResponse>(request.ToPostUrl(), fileToUpload, request, fieldName);
        }

        public static TResponse PostFileWithRequest<TResponse>(this IRestClient client,
            string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request, string fieldName = "upload")
        {
            using (FileStream fileStream = fileToUpload.OpenRead())
            {
                return client.PostFileWithRequest<TResponse>(relativeOrAbsoluteUrl, fileStream, fileToUpload.Name, request, fieldName);
            }
        }

        public static void PopulateRequestMetadata(this IHasSessionId client, object request)
        {
            if (client.SessionId != null)
            {
                if (request is IHasSessionId hasSession && hasSession.SessionId == null)
                    hasSession.SessionId = client.SessionId;
            }
            if (client is IHasVersion clientVersion && clientVersion.Version > 0)
            {
                if (request is IHasVersion hasVersion && hasVersion.Version <= 0)
                    hasVersion.Version = clientVersion.Version;
            }
        }

        public static Dictionary<string, string> ToDictionary(this CookieContainer cookies, string baseUri)
        {
            var to = new Dictionary<string, string>();
            if (cookies == null)
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
            if (!(client is IHasCookieContainer hasCookies))
                throw new NotSupportedException("Client does not implement IHasCookieContainer");

            hasCookies.CookieContainer.SetCookie(baseUri, name, value, expiresAt, path, httpOnly, secure);
        }

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

        public static string GetTokenCookie(this IServiceClient client)
        {
            client.GetCookieValues().TryGetValue("ss-tok", out var token);
            return token;
        }

        public static string GetTokenCookie(this CookieContainer cookies, string baseUri)
        {
            cookies.ToDictionary(baseUri).TryGetValue("ss-tok", out var token);
            return token;
        }

        public static void SetTokenCookie(this IServiceClient client, string token)
        {
            if (token == null)
                return;

            client.SetCookie("ss-tok", token, expiresIn: TimeSpan.FromDays(365 * 20));
        }

        public static void SetTokenCookie(this CookieContainer cookies, string baseUri, string token)
        {
            if (token == null)
                return;

            cookies.SetCookie(new Uri(baseUri), "ss-tok", token,
                expiresAt: DateTime.UtcNow.Add(TimeSpan.FromDays(365 * 20)));
        }

        public static void SetUserAgent(this HttpWebRequest req, string userAgent)
        {
            PclExport.Instance.SetUserAgent(req, userAgent);
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
        string UserName { get; }
        string Password { get; }
        bool AlwaysSendBasicAuthHeader { get; }
        int Version { get; }
        string SessionId { get; }

        string ResolveTypedUrl(string httpMethod, object requestDto);
        string ResolveUrl(string httpMethod, string relativeOrAbsoluteUrl);
    }

    public delegate string UrlResolverDelegate(IServiceClientMeta client, string httpMethod, string relativeOrAbsoluteUrl);
    public delegate string TypedUrlResolverDelegate(IServiceClientMeta client, string httpMethod, object requestDto);

    public delegate object ResultsFilterDelegate(Type responseType, string httpMethod, string requestUri, object request);

    public delegate void ResultsFilterResponseDelegate(WebResponse webResponse, object response, string httpMethod, string requestUri, object request);

    public delegate object ExceptionFilterDelegate(WebException webEx, WebResponse webResponse, string requestUri, Type responseType);
}
