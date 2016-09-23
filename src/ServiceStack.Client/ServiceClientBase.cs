// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

#if !(__IOS__ || SL5)
#endif

#if SL5SendOneWay
using ServiceStack.Text;
#endif

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
        private static Action<HttpWebRequest> globalRequestFilter;
        public static Action<HttpWebRequest> GlobalRequestFilter
        {
            get
            {
                return globalRequestFilter;
            }
            set
            {
                globalRequestFilter = value;
                AsyncServiceClient.GlobalRequestFilter = value;
            }
        }

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// This response action is executed globally.
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        private static Action<HttpWebResponse> globalResponseFilter;
        public static Action<HttpWebResponse> GlobalResponseFilter
        {
            get
            {
                return globalResponseFilter;
            }
            set
            {
                globalResponseFilter = value;
                AsyncServiceClient.GlobalResponseFilter = value;
            }
        }

        /// <summary>
        /// Gets the collection of headers to be added to outgoing requests.
        /// </summary>
        public INameValueCollection Headers { get; private set; }

        public const string DefaultHttpMethod = HttpMethods.Post;
        public static string DefaultUserAgent = "ServiceStack .NET Client " + Env.ServiceStackVersion;

        readonly AsyncServiceClient asyncClient;

        protected ServiceClientBase()
        {
            this.HttpMethod = DefaultHttpMethod;
            this.Headers = PclExportClient.Instance.NewNameValueCollection();

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

            asyncClient.HandleCallbackOnUiThread = this.HandleCallbackOnUiThread = true;
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

        private class AccessToken
        {
            private string token;
            internal static readonly AccessToken __accessToken =
                new AccessToken("lUjBZNG56eE9yd3FQdVFSTy9qeGl5dlI5RmZwamc4U05udl000");
            private AccessToken(string token)
            {
                this.token = token;
            }
        }

        /// <summary>
        /// Whether to Accept Gzip,Deflate Content-Encoding and to auto decompress responses
        /// </summary>
        private bool disableAutoCompression;
        public bool DisableAutoCompression
        {
            get { return disableAutoCompression; }
            set
            {
                disableAutoCompression = value;
                asyncClient.DisableAutoCompression = value;
            }
        }

        /// <summary>
        /// The user name for basic authentication
        /// </summary>
        private string username;
        public string UserName
        {
            get { return username; }
            set
            {
                username = value;
                asyncClient.UserName = value;
            }
        }

        /// <summary>
        /// The password for basic authentication
        /// </summary>
        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                asyncClient.Password = value;
            }
        }

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
        private string bearerToken;
        public string BearerToken
        {
            get { return bearerToken; }
            set
            {
                bearerToken = value;
                asyncClient.BearerToken = value;
            }
        }

        public string BaseUri { get; set; }

        public abstract string Format { get; }

        public string SyncReplyBaseUri { get; set; }

        public string AsyncOneWayBaseUri { get; set; }

        public int Version { get; set; }
        public string SessionId { get; set; }

        private string userAgent;
        public string UserAgent
        {
            get
            {
                return userAgent;
            }
            set
            {
                userAgent = value;
                asyncClient.UserAgent = value;
            }
        }

        private TimeSpan? timeout;
        public TimeSpan? Timeout
        {
            get { return this.timeout; }
            set
            {
                this.timeout = value;
                this.asyncClient.Timeout = value;
            }
        }

        private TimeSpan? readWriteTimeout;
        public TimeSpan? ReadWriteTimeout
        {
            get { return this.readWriteTimeout; }
            set
            {
                this.readWriteTimeout = value;
                // TODO implement ReadWriteTimeout in asyncClient
                //this.asyncClient.ReadWriteTimeout = value;
            }
        }

        public virtual string Accept
        {
            get { return ContentType; }
        }

        public abstract string ContentType { get; }

        public string HttpMethod { get; set; }

        /// <summary>
        /// Whether to execute async callbacks on the same Synchronization Context it was called from.
        /// </summary>
        public bool CaptureSynchronizationContext
        {
            get { return asyncClient.CaptureSynchronizationContext; }
            set { asyncClient.CaptureSynchronizationContext = value; }
        }

        public bool HandleCallbackOnUiThread
        {
            get { return asyncClient.HandleCallbackOnUiThread; }
            set { asyncClient.HandleCallbackOnUiThread = value; }
        }

        public bool EmulateHttpViaPost
        {
            get { return asyncClient.EmulateHttpViaPost; }
            set { asyncClient.EmulateHttpViaPost = value; }
        }

        public ProgressDelegate OnDownloadProgress
        {
            get { return asyncClient.OnDownloadProgress; }
            set { asyncClient.OnDownloadProgress = value; }
        }

        public ProgressDelegate OnUploadProgress
        {
            get { return asyncClient.OnUploadProgress; }
            set { asyncClient.OnUploadProgress = value; }
        }

        private bool shareCookiesWithBrowser;
        public bool ShareCookiesWithBrowser
        {
            get { return this.shareCookiesWithBrowser; }
            set { asyncClient.ShareCookiesWithBrowser = this.shareCookiesWithBrowser = value; }
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
            get { return this.credentials; }
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
        private bool alwaysSendBasicAuthHeader;
        public bool AlwaysSendBasicAuthHeader
        {
            get { return alwaysSendBasicAuthHeader; }
            set { asyncClient.AlwaysSendBasicAuthHeader = alwaysSendBasicAuthHeader = value; }
        }

        /// <summary>
        /// Specifies if cookies should be stored
        /// </summary>
        private bool storeCookies;
        public bool StoreCookies
        {
            get { return storeCookies; }
            set { asyncClient.StoreCookies = storeCookies = value; }
        }

        private CookieContainer cookieContainer;
        public CookieContainer CookieContainer
        {
            get { return cookieContainer; }
            set { asyncClient.CookieContainer = cookieContainer = value; }
        }

        private bool allowAutoRedirect = true;
        public bool AllowAutoRedirect
        {
            get { return allowAutoRedirect; }
            set
            {
                allowAutoRedirect = value;
                // TODO: Implement for async client.
                // asyncClient.AllowAutoRedirect = value;
            }
        }

        /// <summary>
        /// Called before request resend, when the initial request required authentication
        /// </summary>
        private Action onAuthenticationRequired { get; set; }
        public Action OnAuthenticationRequired
        {
            get
            {
                return onAuthenticationRequired;
            }
            set
            {
                onAuthenticationRequired = value;
                asyncClient.OnAuthenticationRequired = value;
            }
        }

        /// <summary>
        /// The request filter is called before any request.
        /// This request filter only works with the instance where it was set (not global).
        /// </summary>
        private Action<HttpWebRequest> requestFilter { get; set; }
        public Action<HttpWebRequest> RequestFilter
        {
            get
            {
                return requestFilter;
            }
            set
            {
                requestFilter = value;
                asyncClient.RequestFilter = value;
            }
        }

        /// <summary>
        /// The ResultsFilter is called before the Request is sent allowing you to return a cached response.
        /// </summary>
        private ResultsFilterDelegate resultsFilter;
        public ResultsFilterDelegate ResultsFilter
        {
            get
            {
                return resultsFilter;
            }
            set
            {
                resultsFilter = value;
                asyncClient.ResultsFilter = value;
            }
        }

        /// <summary>
        /// The ResultsFilterResponse is called before returning the response allowing responses to be cached.
        /// </summary>
        private ResultsFilterResponseDelegate resultsFilterResponse;
        public ResultsFilterResponseDelegate ResultsFilterResponse
        {
            get
            {
                return resultsFilterResponse;
            }
            set
            {
                resultsFilterResponse = value;
                asyncClient.ResultsFilterResponse = value;
            }
        }

        /// <summary>
        /// Called with requestUri, ResponseType when server returns 304 NotModified
        /// </summary>
        public ExceptionFilterDelegate exceptionFilter;
        public ExceptionFilterDelegate ExceptionFilter
        {
            get
            {
                return exceptionFilter;
            }
            set
            {
                exceptionFilter = value;
                asyncClient.ExceptionFilter = value;
            }
        }

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        private Action<HttpWebResponse> responseFilter { get; set; }
        public Action<HttpWebResponse> ResponseFilter
        {
            get
            {
                return responseFilter;
            }
            set
            {
                responseFilter = value;
                asyncClient.ResponseFilter = value;
            }
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
        public virtual string GetUrl(string relativeOrAbsoluteUrl)
        {
            return ToAbsoluteUrl(relativeOrAbsoluteUrl);
        }

        internal void AsyncSerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            SerializeRequestToStream(requestContext, request, stream);
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
                var webResponse = PclExport.Instance.GetResponse(client);
                return HandleResponse<List<TResponse>>(webResponse);
            }
            catch (Exception ex)
            {
                List<TResponse> response;

                if (!HandleResponseException(ex,
                    requests,
                    requestUri,
                    () => SendRequest(HttpMethods.Post, requestUri, requests),
                    c => PclExport.Instance.GetResponse(c),
                    out response))
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
                var webResponse = PclExport.Instance.GetResponse(client);
                ApplyWebResponseFilters(webResponse);

                var response = GetResponse<TResponse>(webResponse);
                if (ResultsFilterResponse != null)
                {
                    ResultsFilterResponse(webResponse, response, httpMethod, requestUri, request);
                }

                DisposeIfRequired<TResponse>(webResponse);

                return response;
            }
            catch (Exception ex)
            {
                TResponse response;

                if (!HandleResponseException(ex,
                    request,
                    requestUri,
                    () => SendRequest(HttpMethods.Post, requestUri, request),
                    c => PclExport.Instance.GetResponse(c),
                    out response))
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
                        || OnAuthenticationRequired != null))
                {
                    if (OnAuthenticationRequired != null)
                        OnAuthenticationRequired();

                    var client = createWebRequest();

                    HandleAuthException(ex, client);

                    var webResponse = getResponse(client);
                    response = HandleResponse<TResponse>(webResponse);

                    return true;
                }
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

            if (ExceptionFilter != null && webEx != null && webEx.Response != null)
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
            if (webEx != null && webEx.Response != null)
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
                    client.AddAuthInfo(this.UserName, this.Password, authInfo);
                }
            }
        }

        readonly ConcurrentDictionary<Type, Action<Exception, string>> ResponseHandlers
            = new ConcurrentDictionary<Type, Action<Exception, string>>();

        private void ThrowResponseTypeException<TResponse>(object request, Exception ex, string requestUri)
        {
            var responseType = WebRequestUtils.GetErrorResponseDtoType<TResponse>(request);
            Action<Exception, string> responseHandler;
            if (!ResponseHandlers.TryGetValue(responseType, out responseHandler))
            {
                var mi = GetType().GetInstanceMethod("ThrowWebServiceException")
                    .MakeGenericMethod(new[] { responseType });

                responseHandler = (Action<Exception, string>)mi.CreateDelegate(
                    typeof(Action<Exception, string>), this);

                ResponseHandlers[responseType] = responseHandler;
            }
            responseHandler(ex, requestUri);
        }

        public void ThrowWebServiceException<TResponse>(Exception ex, string requestUri)
        {
            var webEx = ex as WebException;
            if (webEx != null && webEx.Response != null
#if !(SL5 || PCL || NETSTANDARD1_1)
                 && webEx.Status == WebExceptionStatus.ProtocolError
#endif
            )
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
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
                    if (string.IsNullOrEmpty(errorResponse.ContentType) || errorResponse.ContentType.MatchesContentType(ContentType))
                    {
                        var bytes = errorResponse.GetResponseStream().ReadFully();
                        var stream = MemoryStreamFactory.GetStream(bytes);
                        serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                        serviceEx.ResponseDto = DeserializeFromStream<TResponse>(stream);

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
                    throw new WebServiceException(errorResponse.StatusDescription, innerEx)
                    {
                        StatusCode = (int)errorResponse.StatusCode,
                        StatusDescription = errorResponse.StatusDescription,
                        ResponseBody = serviceEx.ResponseBody
                    };
                }

                //Escape deserialize exception handling and throw here
                throw serviceEx;
            }

            var authEx = ex as AuthenticationException;
            if (authEx != null)
            {
                throw WebRequestUtils.CreateCustomException(requestUri, authEx);
            }
        }

        private WebRequest SendRequest(string httpMethod, string requestUri, object request)
        {
            return PrepareWebRequest(httpMethod, requestUri, request, client =>
            {
                using (var requestStream = PclExport.Instance.GetRequestStream(client))
                {
                    SerializeRequestToStream(null, request, requestStream);
                }
            });
        }

        private void SerializeRequestToStream(IRequest requestContext, object request, Stream requestStream)
        {
            var str = request as string;
            var bytes = request as byte[];
            var stream = request as Stream;
            if (str != null)
                requestStream.Write(str);
            else if (bytes != null)
                requestStream.Write(bytes, 0, bytes.Length);
            else if (stream != null)
                stream.WriteTo(requestStream);
            else
                SerializeToStream(null, request, requestStream);
        }

        private WebRequest PrepareWebRequest(string httpMethod, string requestUri, object request, Action<HttpWebRequest> sendRequestAction)
        {
            if (httpMethod == null)
                throw new ArgumentNullException("httpMethod");

            this.PopulateRequestMetadata(request);

            if (!httpMethod.HasRequestBody() && request != null)
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

#if !SL5
                if (Proxy != null) client.Proxy = Proxy;
#endif
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

                if (httpMethod.HasRequestBody())
                {
                    client.ContentType = ContentType;

                    if (sendRequestAction != null)
                        sendRequestAction(client);
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

            if (ResponseFilter != null)
                ResponseFilter((HttpWebResponse)webResponse);

            if (GlobalResponseFilter != null)
                GlobalResponseFilter((HttpWebResponse)webResponse);
        }

        private void ApplyWebRequestFilters(HttpWebRequest client)
        {
            if (RequestFilter != null)
                RequestFilter(client);

            if (GlobalRequestFilter != null)
                GlobalRequestFilter(client);
        }

        private byte[] DownloadBytes(string httpMethod, string requestUri, object request)
        {
            var webRequest = SendRequest(httpMethod, requestUri, request);
            using (var response = PclExport.Instance.GetResponse(webRequest))
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
                HttpWebResponse response;

                if (!HandleResponseException(
                    ex,
                    requestDto,
                    requestUri,
                    () => SendRequest(httpMethod, requestUri, requestDto),
                    c => PclExport.Instance.GetResponse(c),
                    out response))
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
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? request : null;
            return asyncClient.SendAsync<TResponse>(httpVerb, ResolveUrl(httpVerb, relativeOrAbsoluteUrl), requestBody);
        }

        public virtual Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return asyncClient.SendAsync<byte[]>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }


        public virtual void CancelAsync()
        {
            asyncClient.CancelAsync();
        }

        public virtual TResponse Send<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, object request)
        {
            var requestUri = ToAbsoluteUrl(relativeOrAbsoluteUrl);

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, requestUri, request);
                if (response is TResponse)
                    return (TResponse)response;
            }

            var client = SendRequest(httpMethod, requestUri, request);

            try
            {
                var webResponse = PclExport.Instance.GetResponse(client);
                ApplyWebResponseFilters(webResponse);

                var response = GetResponse<TResponse>(webResponse);
                if (ResultsFilterResponse != null)
                {
                    ResultsFilterResponse(webResponse, response, httpMethod, requestUri, request);
                }

                DisposeIfRequired<TResponse>(webResponse);

                return response;
            }
            catch (Exception ex)
            {
                TResponse response;

                if (!HandleResponseException(
                    ex,
                    request,
                    requestUri,
                    () => SendRequest(httpMethod, requestUri, request),
                    c => PclExport.Instance.GetResponse(c),
                    out response))
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
            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
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
            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return Send<TResponse>(httpVerb, ResolveTypedUrl(httpVerb, requestDto), requestBody);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, object requestDto)
        {
            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
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
                        outputStream.Write(boundary);
                        outputStream.Write(fileCount != files.Length - 1 ? newLine : "--");
                    }
                }

                return webRequest;
            };

            try
            {
                var webRequest = createWebRequest();
                var webResponse = PclExport.Instance.GetResponse(webRequest);
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                TResponse response;

                // restore original position before retry
                files[fileCount - 1].Stream.Seek(currentStreamPosition, SeekOrigin.Begin);

                if (!HandleResponseException(
                    ex, request, requestUri, createWebRequest,
                    c => PclExport.Instance.GetResponse(c),
                    out response))
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

            Func<WebRequest> createWebRequest = () =>
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
                        outputStream.Write("Content-Disposition: form-data;name=\"{0}\"{1}".FormatWith(key, newLine));
                        outputStream.Write("Content-Type: text/plain;charset=utf-8{0}{1}".FormatWith(newLine, newLine));
                        outputStream.Write(nameValueCollection[key] + newLine);
                    }

                    outputStream.Write(boundary + newLine);
                    outputStream.Write("Content-Disposition: form-data;name=\"{0}\";filename=\"{1}\"{2}{3}".FormatWith(fieldName, fileName, newLine, newLine));
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
            };

            try
            {
                var webRequest = createWebRequest();
                var webResponse = PclExport.Instance.GetResponse(webRequest);
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                TResponse response;

                // restore original position before retry
                fileToUpload.Seek(currentStreamPosition, SeekOrigin.Begin);

                if (!HandleResponseException(
                    ex, request, requestUri, createWebRequest,
                    c => PclExport.Instance.GetResponse(c),
                    out response))
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
            Func<WebRequest> createWebRequest = () => PrepareWebRequest(HttpMethods.Post, requestUri, null, null);

            try
            {
                var webRequest = createWebRequest();
                webRequest.UploadFile(fileToUpload, fileName, mimeType);
                var webResponse = PclExport.Instance.GetResponse(webRequest);
                return HandleResponse<TResponse>(webResponse);
            }
            catch (Exception ex)
            {
                TResponse response;

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
                    out response))
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

            using (webResponse) {}
        }

        protected TResponse GetResponse<TResponse>(WebResponse webResponse)
        {
            //Callee Needs to dispose of response manually
            if (typeof(TResponse) == typeof(HttpWebResponse) && webResponse is HttpWebResponse)
            {
                return (TResponse)Convert.ChangeType(webResponse, typeof(TResponse), null);
            }
            if (typeof(TResponse) == typeof(Stream))
            {
                return (TResponse)(object)webResponse.GetResponseStream();
            }

            using (var responseStream = webResponse.GetResponseStream())
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
#if !(NETFX_CORE || SL5 || PCL || NETSTANDARD1_1)
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
#endif

        public static void PopulateRequestMetadata(this IHasSessionId client, object request)
        {
            if (client.SessionId != null)
            {
                var hasSession = request as IHasSessionId;
                if (hasSession != null && hasSession.SessionId == null)
                    hasSession.SessionId = client.SessionId;
            }
            var clientVersion = client as IHasVersion;
            if (clientVersion != null && clientVersion.Version > 0)
            {
                var hasVersion = request as IHasVersion;
                if (hasVersion != null && hasVersion.Version <= 0)
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

        [Obsolete("Use: using (client.Get<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Get(this IRestClient client, object request)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Get(request);
        }

        [Obsolete("Use: using (client.Delete<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Delete(this IRestClient client, object request)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Delete(request);
        }

        [Obsolete("Use: using (client.Post<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Post(this IRestClient client, object request)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Post(request);
        }

        [Obsolete("Use: using (client.Put<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Put(this IRestClient client, object request)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Put(request);
        }

        [Obsolete("Use: using (client.Patch<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Patch(this IRestClient client, object request)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Patch(request);
        }

        [Obsolete("Use: using (client.CustomMethod<HttpWebResponse>(httpVerb, request) { }")]
        public static HttpWebResponse CustomMethod(this IRestClient client, string httpVerb, object requestDto)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.CustomMethod(httpVerb, requestDto);
        }

        [Obsolete("Use: using (client.Head<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Head(this IRestClient client, IReturn requestDto)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Head(requestDto);
        }

        [Obsolete("Use: using (client.Head<HttpWebResponse>(request) { }")]
        public static HttpWebResponse Head(this IRestClient client, object requestDto)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Head(requestDto);
        }

        [Obsolete("Use: using (client.Head<HttpWebResponse>(relativeOrAbsoluteUrl) { }")]
        public static HttpWebResponse Head(this IRestClient client, string relativeOrAbsoluteUrl)
        {
            var c = client as ServiceClientBase;
            if (c == null)
                throw new NotSupportedException();
            return c.Head(relativeOrAbsoluteUrl);
        }

        public static void SetCookie(this IServiceClient client, Uri baseUri, string name, string value,
            DateTime? expiresAt = null, string path = "/",
            bool? httpOnly = null, bool? secure = null)
        {
            var hasCookies = client as IHasCookieContainer;
            if (hasCookies == null)
                throw new NotSupportedException("Client does not implement IHasCookieContainer");

            var cookie = new Cookie(name, value, path);
            if (expiresAt != null)
                cookie.Expires = expiresAt.Value;
            if (path != null)
                cookie.Path = path;
            if (httpOnly != null)
                cookie.HttpOnly = httpOnly.Value;
            if (secure != null)
                cookie.Secure = secure.Value;

            hasCookies.CookieContainer.Add(baseUri, cookie);
        }

        public static string GetSessionId(this IServiceClient client)
        {
            string sessionId;
            client.GetCookieValues().TryGetValue("ss-id", out sessionId);
            return sessionId;
        }

        public static string GetPermanentSessionId(this IServiceClient client)
        {
            string sessionId;
            client.GetCookieValues().TryGetValue("ss-pid", out sessionId);
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

            client.SetCookie("ss-pid", sessionId, expiresIn:TimeSpan.FromDays(365 * 20));
        }

        public static string GetTokenCookie(this IServiceClient client)
        {
            string token;
            client.GetCookieValues().TryGetValue("ss-tok", out token);
            return token;
        }

        public static void SetTokenCookie(this IServiceClient client, string token)
        {
            if (token == null)
                return;

            client.SetCookie("ss-tok", token, expiresIn: TimeSpan.FromDays(365 * 20));
        }

        public static void SetUserAgent(this HttpWebRequest req, string userAgent)
        {
#if !(PCL || NETSTANDARD1_1 || NETSTANDARD1_6)
            req.UserAgent = userAgent;
#else
            req.Headers[HttpRequestHeader.UserAgent] = userAgent;
#endif
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
    }

    public delegate string UrlResolverDelegate(IServiceClientMeta client, string httpMethod, string relativeOrAbsoluteUrl);
    public delegate string TypedUrlResolverDelegate(IServiceClientMeta client, string httpMethod, object requestDto);

    public delegate object ResultsFilterDelegate(Type responseType, string httpMethod, string requestUri, object request);

    public delegate void ResultsFilterResponseDelegate(WebResponse webResponse, object response, string httpMethod, string requestUri, object request);

    public delegate object ExceptionFilterDelegate(WebException webEx, WebResponse webResponse, string requestUri, Type responseType);
}