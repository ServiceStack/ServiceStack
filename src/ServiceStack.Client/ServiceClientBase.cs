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
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

#if !(__IOS__ || SL5)
#endif

#if SL5
using ServiceStack.Text;
#endif

namespace ServiceStack
{

    /**
     * Need to provide async request options
     * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
     */
    public abstract class ServiceClientBase : IServiceClient, IMessageProducer
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

        public const string DefaultHttpMethod = "POST";
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
                Headers = this.Headers,
            };
            this.CookieContainer = new CookieContainer();
            this.StoreCookies = true; //leave
            this.UserAgent = DefaultUserAgent;

            asyncClient.HandleCallbackOnUiThread = this.HandleCallbackOnUiThread = true;
            asyncClient.ShareCookiesWithBrowser = this.ShareCookiesWithBrowser = true;
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

        public string BaseUri { get; set; }

        public abstract string Format { get; }

        public string SyncReplyBaseUri { get; set; }

        public string AsyncOneWayBaseUri { get; set; }

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
        private Action<WebRequest> onAuthenticationRequired { get; set; }
        public Action<WebRequest> OnAuthenticationRequired
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

        internal void AsyncSerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            using (__requestAccess())
            {
                SerializeToStream(requestContext, request, stream);
            }
        }

        public abstract void SerializeToStream(IRequest requestContext, object request, Stream stream);

        public abstract T DeserializeFromStream<T>(Stream stream);

        public abstract StreamDeserializerDelegate StreamDeserializer { get; }

        internal object AsyncDeserializeFromStream(Type type, Stream fromStream)
        {
            using (__requestAccess())
            {
                return StreamDeserializer(type, fromStream);
            }
        }

        protected T Deserialize<T>(string text)
        {
            using (__requestAccess())
            using (var ms = MemoryStreamFactory.GetStream(text.ToUtf8Bytes()))
            {
                return DeserializeFromStream<T>(ms);
            }
        }

        public virtual List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";
            var client = SendRequest(requestUri, requests);

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

        public virtual TResponse Send<TResponse>(IReturn<TResponse> request)
        {
            return Send<TResponse>((object)request);
        }

        public virtual void Send(IReturnVoid request)
        {
            SendOneWay(request);
        }

        public virtual TResponse Send<TResponse>(object request)
        {
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name;
            var client = SendRequest(requestUri, request);

            try
            {
                var webResponse = PclExport.Instance.GetResponse(client);
                return HandleResponse<TResponse>(webResponse);
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
            try
            {
                if (WebRequestUtils.ShouldAuthenticate(ex, this.UserName, this.Password))
                {
                    // adamfowleruk : Check response object to see what type of auth header to add

                    var client = createWebRequest();

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


                    if (OnAuthenticationRequired != null)
                    {
                        OnAuthenticationRequired(client);
                    }

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

            // If this doesn't throw, the calling method 
            // should rethrow the original exception upon
            // return value of false.
            ThrowResponseTypeException<TResponse>(request, ex, requestUri);

            response = default(TResponse);
            return false;
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
#if !(SL5 || PCL)
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
                    if (errorResponse.ContentType.MatchesContentType(ContentType))
                    {
                        var bytes = errorResponse.GetResponseStream().ReadFully();
                        using (__requestAccess())
                        {
                            var stream = MemoryStreamFactory.GetStream(bytes);
                            serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                            serviceEx.ResponseDto = DeserializeFromStream<TResponse>(stream);

                            if (stream.CanRead)
                                stream.Dispose(); //alt ms throws when you dispose twice
                        }
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

        private WebRequest SendRequest(string requestUri, object request)
        {
            return SendRequest(HttpMethod ?? DefaultHttpMethod, requestUri, request);
        }

        private WebRequest SendRequest(string httpMethod, string requestUri, object request)
        {
            return PrepareWebRequest(httpMethod, requestUri, request, client =>
            {
                using (__requestAccess())
                using (var requestStream = PclExport.Instance.GetRequestStream(client))
                {
                    SerializeToStream(null, request, requestStream);
                }
            });
        }

        private WebRequest PrepareWebRequest(string httpMethod, string requestUri, object request, Action<HttpWebRequest> sendRequestAction)
        {
            if (httpMethod == null)
                throw new ArgumentNullException("httpMethod");

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

                if (this.credentials != null) client.Credentials = this.credentials;

                if (null != this.authInfo)
                {
                    client.AddAuthInfo(this.UserName, this.Password, authInfo);
                }
                else
                {
                    if (this.AlwaysSendBasicAuthHeader) 
                        client.AddBasicAuth(this.UserName, this.Password);
                }

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

        protected static IDisposable __requestAccess()
        {
            return LicenseUtils.RequestAccess(AccessToken.__accessToken, LicenseFeature.Client, LicenseFeature.Text);
        }

        public virtual string GetUrl(string relativeOrAbsoluteUrl)
        {
            return relativeOrAbsoluteUrl.StartsWith("http:")
                || relativeOrAbsoluteUrl.StartsWith("https:")
                     ? relativeOrAbsoluteUrl
                     : this.BaseUri.CombineWith(relativeOrAbsoluteUrl);
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

        public void Publish<T>(T requestDto)
        {
            Post(requestDto);
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

            Post(requestDto);
        }

        public virtual void SendOneWay(object requestDto)
        {
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + requestDto.GetType().Name;
            SendOneWay(HttpMethods.Post, requestUri, requestDto);
        }

        public virtual void SendOneWay(string relativeOrAbsoluteUrl, object request)
        {
            SendOneWay(HttpMethods.Post, relativeOrAbsoluteUrl, request);
        }

        public virtual void SendAllOneWay(IEnumerable<object> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + elType.Name + "[]";
            SendOneWay(HttpMethods.Post, requestUri, requests);
        }

        public virtual void SendOneWay(string httpMethod, string relativeOrAbsoluteUrl, object requestDto)
        {
            var requestUri = GetUrl(relativeOrAbsoluteUrl);
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

                using(response){} //auto dispose
            }
        }

        public virtual Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return SendAsync<TResponse>((object)requestDto);
        }

        public virtual Task<TResponse> SendAsync<TResponse>(object requestDto)
        {
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + requestDto.GetType().Name;
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, requestUri, requestDto);
        }

        public virtual Task<HttpWebResponse> SendAsync(IReturnVoid requestDto)
        {
            return SendAsync<HttpWebResponse>(requestDto);
        }

        public virtual Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requests)
        {
            var elType = requests.GetType().GetCollectionType();
            var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + elType.Name + "[]";

            return asyncClient.SendAsync<List<TResponse>>(HttpMethods.Post, requestUri, requests);
        }


        public virtual Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return GetAsync<TResponse>(requestDto.ToUrl(HttpMethods.Get, Format));
        }

        public virtual Task<TResponse> GetAsync<TResponse>(object requestDto)
        {
            return GetAsync<TResponse>(requestDto.ToUrl(HttpMethods.Get, Format));
        }

        public virtual Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Get, GetUrl(relativeOrAbsoluteUrl), null);
        }

        public virtual Task GetAsync(IReturnVoid requestDto)
        {
            return GetAsync<byte[]>(requestDto.ToUrl(HttpMethods.Get, Format));
        }


        public virtual Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return DeleteAsync<TResponse>(requestDto.ToUrl(HttpMethods.Delete, Format));
        }

        public virtual Task<TResponse> DeleteAsync<TResponse>(object requestDto)
        {
            return DeleteAsync<TResponse>(requestDto.ToUrl(HttpMethods.Delete, Format));
        }

        public virtual Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Delete, GetUrl(relativeOrAbsoluteUrl), null);
        }

        public virtual Task DeleteAsync(IReturnVoid requestDto)
        {
            return DeleteAsync<byte[]>(requestDto.ToUrl(HttpMethods.Delete, Format));
        }


        public virtual Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return PostAsync<TResponse>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }

        public virtual Task<TResponse> PostAsync<TResponse>(object requestDto)
        {
            return PostAsync<TResponse>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }

        public virtual Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Post, GetUrl(relativeOrAbsoluteUrl), request);
        }

        public virtual Task PostAsync(IReturnVoid requestDto)
        {
            return PostAsync<byte[]>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }


        public virtual Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return PutAsync<TResponse>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }

        public virtual Task<TResponse> PutAsync<TResponse>(object requestDto)
        {
            return PutAsync<TResponse>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }

        public virtual Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Put, GetUrl(relativeOrAbsoluteUrl), request);
        }

        public virtual Task PutAsync(IReturnVoid requestDto)
        {
            return PutAsync<byte[]>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }


        public virtual Task<TResponse> PatchAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return PatchAsync<TResponse>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }

        public virtual Task<TResponse> PatchAsync<TResponse>(object requestDto)
        {
            return PatchAsync<TResponse>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }

        public virtual Task<TResponse> PatchAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return asyncClient.SendAsync<TResponse>(HttpMethods.Patch, GetUrl(relativeOrAbsoluteUrl), request);
        }

        public virtual Task PatchAsync(IReturnVoid requestDto)
        {
            return PatchAsync<byte[]>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }


        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return asyncClient.SendAsync<TResponse>(httpVerb, GetUrl(requestDto.ToUrl(httpVerb, Format)), requestBody);
        }

        public virtual Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return asyncClient.SendAsync<TResponse>(httpVerb, GetUrl(requestDto.ToUrl(httpVerb, Format)), requestBody);
        }

        public virtual Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            if (!HttpMethods.HasVerb(httpVerb))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return asyncClient.SendAsync<byte[]>(httpVerb, GetUrl(requestDto.ToUrl(httpVerb, Format)), requestBody);
        }


        public virtual void CancelAsync()
        {
            asyncClient.CancelAsync();
        }

        public virtual TResponse Send<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, object request)
        {
            var requestUri = GetUrl(relativeOrAbsoluteUrl);
            var client = SendRequest(httpMethod, requestUri, request);

            try
            {
                var webResponse = PclExport.Instance.GetResponse(client);
                return HandleResponse<TResponse>(webResponse);
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


        public virtual void Get(IReturnVoid requestDto)
        {
            Get<byte[]>(requestDto.ToUrl(HttpMethods.Get, Format));
        }

        public virtual HttpWebResponse Get(object requestDto)
        {
            return Get<HttpWebResponse>(requestDto.ToUrl(HttpMethods.Get, Format));
        }

        public virtual HttpWebResponse Get(string relativeOrAbsoluteUrl)
        {
            return Get<HttpWebResponse>(relativeOrAbsoluteUrl);
        }

        public virtual TResponse Get<TResponse>(IReturn<TResponse> requestDto)
        {
            return Get<TResponse>(requestDto.ToUrl(HttpMethods.Get, Format));
        }

        public virtual TResponse Get<TResponse>(object requestDto)
        {
            return Get<TResponse>(requestDto.ToUrl(HttpMethods.Get, Format));
        }

        public virtual TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
        {
            return Send<TResponse>(HttpMethods.Get, relativeOrAbsoluteUrl, null);
        }

        public virtual IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto)
        {
            var query = (IQuery)queryDto;
            QueryResponse<TResponse> response;
            do
            {
                response = Get<QueryResponse<TResponse>>(queryDto.ToUrl(HttpMethods.Get, Format));
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
            Delete<byte[]>(requestDto.ToUrl(HttpMethods.Delete, Format));
        }

        public virtual HttpWebResponse Delete(object requestDto)
        {
            return Delete(requestDto.ToUrl(HttpMethods.Delete, Format));
        }

        public virtual HttpWebResponse Delete(string relativeOrAbsoluteUrl)
        {
            return Delete<HttpWebResponse>(relativeOrAbsoluteUrl);
        }

        public virtual TResponse Delete<TResponse>(IReturn<TResponse> requestDto)
        {
            return Delete<TResponse>(requestDto.ToUrl(HttpMethods.Delete, Format));
        }

        public virtual TResponse Delete<TResponse>(object requestDto)
        {
            return Delete<TResponse>(requestDto.ToUrl(HttpMethods.Delete, Format));
        }

        public virtual TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
        {
            return Send<TResponse>(HttpMethods.Delete, relativeOrAbsoluteUrl, null);
        }


        public virtual void Post(IReturnVoid requestDto)
        {
            Post<byte[]>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }

        public virtual HttpWebResponse Post(object requestDto)
        {
            return Post<HttpWebResponse>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }

        public virtual TResponse Post<TResponse>(IReturn<TResponse> requestDto)
        {
            return Post<TResponse>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }

        public virtual TResponse Post<TResponse>(object requestDto)
        {
            return Post<TResponse>(requestDto.ToUrl(HttpMethods.Post, Format), requestDto);
        }

        public virtual TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return Send<TResponse>(HttpMethods.Post, relativeOrAbsoluteUrl, requestDto);
        }


        public virtual void Put(IReturnVoid requestDto)
        {
            Put<byte[]>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }

        public virtual HttpWebResponse Put(object requestDto)
        {
            return Put<HttpWebResponse>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }

        public virtual TResponse Put<TResponse>(IReturn<TResponse> requestDto)
        {
            return Put<TResponse>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }

        public virtual TResponse Put<TResponse>(object requestDto)
        {
            return Put<TResponse>(requestDto.ToUrl(HttpMethods.Put, Format), requestDto);
        }

        public virtual TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return Send<TResponse>(HttpMethods.Put, relativeOrAbsoluteUrl, requestDto);
        }


        public virtual void Patch(IReturnVoid requestDto)
        {
            Patch<byte[]>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }

        public virtual HttpWebResponse Patch(object requestDto)
        {
            return Patch<HttpWebResponse>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }

        public virtual TResponse Patch<TResponse>(IReturn<TResponse> requestDto)
        {
            return Patch<TResponse>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }

        public virtual TResponse Patch<TResponse>(object requestDto)
        {
            return Patch<TResponse>(requestDto.ToUrl(HttpMethods.Patch, Format), requestDto);
        }

        public virtual TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return Send<TResponse>(HttpMethods.Patch, relativeOrAbsoluteUrl, requestDto);
        }


        public virtual void CustomMethod(string httpVerb, IReturnVoid requestDto)
        {
            CustomMethod<byte[]>(httpVerb, requestDto.ToUrl(httpVerb, Format), requestDto);
        }

        public virtual HttpWebResponse CustomMethod(string httpVerb, object requestDto)
        {
            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return CustomMethod<HttpWebResponse>(httpVerb, requestDto.ToUrl(httpVerb, Format), requestBody);
        }

        public virtual HttpWebResponse CustomMethod(string httpVerb, string relativeOrAbsoluteUrl, object requestDto)
        {
            if (!HttpMethods.AllVerbs.Contains(httpVerb.ToUpper()))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            return Send<HttpWebResponse>(httpVerb, relativeOrAbsoluteUrl, requestDto);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return CustomMethod<TResponse>(httpVerb, requestDto.ToUrl(httpVerb, Format), requestBody);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, object requestDto)
        {
            var requestBody = httpVerb.HasRequestBody() ? requestDto : null;
            return CustomMethod<TResponse>(httpVerb, requestDto.ToUrl(httpVerb, Format), requestBody);
        }

        public virtual TResponse CustomMethod<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object requestDto = null)
        {
            if (!HttpMethods.AllVerbs.Contains(httpVerb.ToUpper()))
                throw new NotSupportedException("Unknown HTTP Method is not supported: " + httpVerb);

            return Send<TResponse>(httpVerb, relativeOrAbsoluteUrl, requestDto);
        }


        public virtual HttpWebResponse Head(IReturn requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, requestDto.ToUrl(HttpMethods.Head), requestDto);
        }

        public virtual HttpWebResponse Head(object requestDto)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, requestDto.ToUrl(HttpMethods.Head), requestDto);
        }

        public virtual HttpWebResponse Head(string relativeOrAbsoluteUrl)
        {
            return Send<HttpWebResponse>(HttpMethods.Head, relativeOrAbsoluteUrl, null);
        }

        public virtual TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            return PostFileWithRequest<TResponse>(request.ToPostUrl(), fileToUpload, fileName, request, fieldName);
        }

        public virtual TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            var requestUri = GetUrl(relativeOrAbsoluteUrl);
            var currentStreamPosition = fileToUpload.Position;

            Func<WebRequest> createWebRequest = () =>
            {
                var webRequest = PrepareWebRequest(HttpMethods.Post, requestUri, null, null);

                var queryString = QueryStringSerializer.SerializeToString(request);

                var nameValueCollection = PclExportClient.Instance.ParseQueryString(queryString);
                var boundary = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
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
            var requestUri = GetUrl(relativeOrAbsoluteUrl);
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

            //Callee Needs to dispose of response manually
            if (typeof(TResponse) == typeof(HttpWebResponse) && (webResponse is HttpWebResponse))
            {
                return (TResponse)Convert.ChangeType(webResponse, typeof(TResponse), null);
            }
            if (typeof(TResponse) == typeof(Stream)) 
            {
                return (TResponse)(object)webResponse.GetResponseStream();
            }

            using (webResponse)
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

                using (__requestAccess())
                {
                    var response = DeserializeFromStream<TResponse>(responseStream);
                    return response;
                }
            }
        }

        public void Dispose() { }
    }

    public static class ServiceClientExtensions
    {
#if !(NETFX_CORE || SL5 || PCL)
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
        
    }
}
