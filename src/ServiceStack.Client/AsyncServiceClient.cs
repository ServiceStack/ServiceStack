// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    /**
     * Need to provide async request options
     * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
     */

    public partial class AsyncServiceClient : IHasSessionId, IHasBearerToken, IHasVersion
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncServiceClient));
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        //private HttpWebRequest webRequest = null;
        private AuthenticationInfo authInfo = null;

        /// <summary>
        /// The request filter is called before any request.
        /// This request filter is executed globally.
        /// </summary>
        public static Action<HttpWebRequest> GlobalRequestFilter { get; set; }

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// This response action is executed globally.
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        public static Action<HttpWebResponse> GlobalResponseFilter { get; set; }

        /// <summary>
        /// Called before request resend, when the initial request required authentication
        /// </summary>
        public Action OnAuthenticationRequired { get; set; }

        public string RefreshToken { get; set; }

        public string RefreshTokenUri { get; set; }
        public bool EnableAutoRefreshToken { get; set; }
        
        public static int BufferSize = 8192;

        public ICredentials Credentials { get; set; }

        public bool AlwaysSendBasicAuthHeader { get; set; }

        public bool StoreCookies { get; set; }

        public NameValueCollection Headers { get; set; }

        public CookieContainer CookieContainer { get; set; }

#if NET6_0_OR_GREATER
        public System.Net.Http.HttpClient HttpClient { get; set; }
#endif
        
        /// <summary>
        /// The request filter is called before any request.
        /// This request filter only works with the instance where it was set (not global).
        /// </summary>
        public Action<HttpWebRequest> RequestFilter { get; set; }

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        public Action<HttpWebResponse> ResponseFilter { get; set; }

        /// <summary>
        /// The ResultsFilter is called before the Request is sent allowing you to return a cached response.
        /// </summary>
        public ResultsFilterDelegate ResultsFilter { get; set; }

        /// <summary>
        /// The ResultsFilterResponse is called before returning the response allowing responses to be cached.
        /// </summary>
        public ResultsFilterResponseDelegate ResultsFilterResponse { get; set; }

        /// <summary>
        /// Called with requestUri, ResponseType when server returns 304 NotModified
        /// </summary>
        public ExceptionFilterDelegate ExceptionFilter { get; set; }

        public string BaseUri { get; set; }
        public bool DisableAutoCompression { get; set; }

        public string RequestCompressionType { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public void SetCredentials(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        public TimeSpan? Timeout { get; set; }

        public string ContentType { get; set; }

        public StreamSerializerDelegate StreamSerializer { get; set; }

        public StreamDeserializerDelegate StreamDeserializer { get; set; }

        public string UserAgent { get; set; }

        public bool EmulateHttpViaPost { get; set; }

        public ProgressDelegate OnDownloadProgress { get; set; }

        public ProgressDelegate OnUploadProgress { get; set; }

        public bool ShareCookiesWithBrowser { get; set; }

        public IWebProxy Proxy { get; set; }

        public int Version { get; set; }

        public string SessionId { get; set; }

        public string BearerToken { get; set; }

        public StringBuilder HttpLog { get; set; }
        public Action<StringBuilder> HttpLogFilter { get; set; }

        public static bool DisableTimer { get; set; }

        public Dictionary<string, string> GetCookieValues()
        {
            return CookieContainer.ToDictionary(BaseUri);
        }

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token = default(CancellationToken))
        {
            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, absoluteUrl, request);
                if (response is TResponse typedResponse)
                    return typedResponse.InTask();
            }

            return SendWebRequestAsync<TResponse>(httpMethod, absoluteUrl, request, token);
        }

        private async Task<T> SendWebRequestAsync<T>(string httpMethod, string absoluteUrl, object request, CancellationToken token, bool recall = false)
        {
            if (httpMethod == null)
                throw new ArgumentNullException(nameof(httpMethod));

            this.PopulateRequestMetadata(request);

            var requestUri = absoluteUrl;
            var hasQueryString = request != null && !HttpUtils.HasRequestBody(httpMethod);
            if (hasQueryString)
            {
                var queryString = QueryStringSerializer.SerializeToString(request);
                if (!string.IsNullOrEmpty(queryString))
                {
                    requestUri += "?" + queryString;
                }
            }

            var webReq = this.CreateHttpWebRequest(requestUri);
            if (webReq != null && Proxy != null) webReq.Proxy = Proxy;

            var timedOut = false;
            ITimer timer = null;
            timer = PclExportClient.Instance.CreateTimer(state =>
            {
                timedOut = true;
                webReq?.Abort();
                webReq = null;
                timer?.Cancel();
                timer = null;
            }, this.Timeout.GetValueOrDefault(DefaultTimeout), this);

            Exception ResolveException(Exception ex)
            {
                if (token.IsCancellationRequested)
                    return new OperationCanceledException(token);
                if (timedOut)
                    return PclExportClient.Instance.CreateTimeoutException(ex, "The request timed out");
                return ex;
            }

            bool returningWebResponse = false;
            HttpWebResponse webRes = null;
            T Complete(T response)
            {
                timer.Cancel();
                PclExportClient.Instance.SynchronizeCookies(this);
                ResultsFilterResponse?.Invoke(webRes, response, httpMethod, absoluteUrl, request);
                return response;
            }

            webReq.Accept = ContentType;

            if (this.EmulateHttpViaPost)
            {
                webReq.Method = "POST";
                webReq.Headers[HttpHeaders.XHttpMethodOverride] = httpMethod;
            }
            else
            {
                webReq.Method = httpMethod;
            }

            PclExportClient.Instance.AddHeader(webReq, Headers);
            PclExport.Instance.Config(webReq, userAgent: UserAgent);

            if (this.authInfo != null && !string.IsNullOrEmpty(this.UserName))
                webReq.AddAuthInfo(this.UserName, this.Password, authInfo);
            else if (this.BearerToken != null)
                webReq.Headers[HttpHeaders.Authorization] = "Bearer " + this.BearerToken;
            else if (this.Credentials != null)
                webReq.Credentials = this.Credentials;
            else if (this.AlwaysSendBasicAuthHeader)
                webReq.AddBasicAuth(this.UserName, this.Password);

            if (!DisableAutoCompression)
            {
                PclExport.Instance.AddCompression(webReq);
            }

            ApplyWebRequestFilters(webReq);

            try
            {
                if (HttpUtils.HasRequestBody(webReq.Method))
                {
                    webReq.ContentType = ContentType;

                   if (RequestCompressionType != null)
                        webReq.Headers[HttpHeaders.ContentEncoding] = RequestCompressionType;

                   if (HttpLog != null)
                       webReq.AppendHttpRequestHeaders(HttpLog, new Uri(BaseUri));
                
                   using var requestStream = await webReq.GetRequestStreamAsync().ConfigAwait();
                   token.ThrowIfCancellationRequested();
                   
                   if (request != null)
                   {
                       StreamSerializer(null, request, requestStream);
                   }
                }
                else
                {
                    if (HttpLog != null)
                        webReq.AppendHttpRequestHeaders(HttpLog, new Uri(BaseUri));
                }
                HttpLog?.AppendLine();
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Error Sending Request: {ex.Message}", ex);

                throw HandleResponseError<T>(ResolveException(ex), requestUri, request);
            }

            try
            {
                webRes = (HttpWebResponse) await webReq.GetResponseAsync().ConfigAwait();
                {
                    token.ThrowIfCancellationRequested();

                    ApplyWebResponseFilters(webRes);

                    returningWebResponse = typeof(T) == typeof(HttpWebResponse);
                    if (returningWebResponse)
                        return Complete((T) (object) webRes);

                    var responseStream = webRes.ResponseStream();

                    var responseBodyLength = webRes.ContentLength;
                    var bufferRead = new byte[BufferSize];

                    var totalRead = 0;
                    int read;
                    var ms = new MemoryStream(); // can get returned, do not dispose

                    while ((read = await responseStream.ReadAsync(bufferRead, 0, bufferRead.Length, token).ConfigAwait()) != 0)
                    {
                        await ms.WriteAsync(bufferRead, 0, read, token).ConfigAwait();
                        totalRead += read;
                        OnDownloadProgress?.Invoke(totalRead, responseBodyLength);
                    }

                    try
                    {
                        ms.Position = 0;

                        if (HttpLog != null)
                        {
                            webRes.AppendHttpResponseHeaders(HttpLog);
                            if (webRes.ContentLength != 0 && webRes.StatusCode != HttpStatusCode.NoContent)
                            {
                                var isBinary = typeof(T) == typeof(Stream) || typeof(T) == typeof(byte[]) || ContentType.IsBinary();
                                if (isBinary)
                                {
                                    HttpLog.Append("(base64) ");
                                    HttpLog.AppendLine(Convert.ToBase64String(ms.ReadFully()));
                                }
                                else
                                {
                                    HttpLog.AppendLine(ms.ReadToEnd());
                                }
                                HttpLog.AppendLine().AppendLine();
                                ms.Position = 0;
                            }
                        }
                        
                        if (typeof(T) == typeof(Stream))
                        {
                            return Complete((T) (object) ms);
                        }
                        else
                        {
                            var stream = ms;
                            try
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    return Complete((T) (object) await stream.ReadToEndAsync().ConfigAwait());
                                }
                                else if (typeof(T) == typeof(byte[]))
                                    return Complete((T) (object) stream.ToArray());
                                else
                                    return Complete((T) this.StreamDeserializer(typeof(T), stream));
                            }
                            finally
                            {
                                if (stream.CanRead)
                                    stream.Dispose(); // Not yet disposed, but could've been.
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Log.IsDebugEnabled)
                            Log.Debug($"Error Reading Response Error: {ex.Message}", ex);

                        throw;
                    }
                    finally
                    {
                        if (HttpLog != null)
                            HttpLogFilter?.Invoke(HttpLog);
                        
                        responseStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var webEx = ex as WebException;
                var firstCall = !recall;
                var hasRefreshTokenCookie = this.CookieContainer.GetRefreshTokenCookie(BaseUri) != null;
                var hasRefreshToken = RefreshToken != null || hasRefreshTokenCookie;
                
                if (firstCall && WebRequestUtils.ShouldAuthenticate(webEx,
                        (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                        || Credentials != null
                        || BearerToken != null
                        || hasRefreshToken
                        || OnAuthenticationRequired != null))
                {
                    try
                    {
                        if (EnableAutoRefreshToken && hasRefreshToken)
                        {
                            var refreshRequest = new GetAccessToken {
                                RefreshToken = hasRefreshTokenCookie ? null : RefreshToken,
                            };                        
                            var uri = this.RefreshTokenUri ?? this.BaseUri.CombineWith(refreshRequest.ToPostUrl());
                        
                            this.BearerToken = null;
                            this.CookieContainer?.DeleteCookie(new Uri(BaseUri), "ss-tok");

                            GetAccessTokenResponse tokenResponse;
                            try
                            {
                                var httpReq = WebRequest.CreateHttp(uri);
                                tokenResponse = (await ServiceClientBase.SendStringToUrlAsync(httpReq, method:HttpMethods.Post, 
                                    requestFilter: req => {
                                        if (hasRefreshTokenCookie) {
                                            req.CookieContainer = CookieContainer;
                                        }
                                    }, requestBody:refreshRequest.ToJson(), accept:MimeTypes.Json, contentType:MimeTypes.Json, token: token)
                                    .ConfigAwait()).FromJson<GetAccessTokenResponse>();
                            }
                            catch (WebException refreshEx)
                            {
                                var webServiceEx = ServiceClientBase.ToWebServiceException(refreshEx,
                                    stream => StreamDeserializer(typeof(T), stream),
                                    ContentType);

                                if (webServiceEx != null)
                                    throw new RefreshTokenException(webServiceEx);

                                throw new RefreshTokenException(refreshEx.Message, refreshEx);
                            }

                            var accessToken = tokenResponse?.AccessToken;
                            var refreshClient = webReq = (HttpWebRequest) WebRequest.Create(requestUri);
                            var tokenCookie = this.CookieContainer.GetTokenCookie(BaseUri);

                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                refreshClient.AddBearerToken(this.BearerToken = accessToken);
                            }
                            else if (tokenCookie != null)
                            {
                                refreshClient.CookieContainer = CookieContainer;
                                refreshClient.CookieContainer.SetTokenCookie(BaseUri, tokenCookie);
                            }
                            else throw new RefreshTokenException("Could not retrieve new AccessToken from: " + uri);

                            return await SendWebRequestAsync<T>(httpMethod, absoluteUrl, request, token, recall: true).ConfigAwait();
                        }

                        OnAuthenticationRequired?.Invoke();

                        var newReq = (HttpWebRequest) WebRequest.Create(requestUri);

                        if (StoreCookies)
                            newReq.CookieContainer = CookieContainer;

                        HandleAuthException(ex, webReq);

                        return await SendWebRequestAsync<T>(httpMethod, absoluteUrl, request, token, recall: true).ConfigAwait();
                    }
                    catch (WebServiceException)
                    {
                        throw;
                    }
                    catch (Exception /*subEx*/)
                    {
                        throw HandleResponseError<T>(ResolveException(ex), requestUri, request);
                    }
                }

                if (ExceptionFilter != null && webEx?.Response != null)
                {
                    var cachedResponse = ExceptionFilter(webEx, webEx.Response, requestUri, typeof(T));
                    if (cachedResponse is T variable)
                        return variable;
                }

                throw HandleResponseError<T>(ResolveException(ex), requestUri, request);
            }
            finally
            {
                if (!returningWebResponse)
                    webRes?.Dispose();
            }
        }

        private Exception HandleResponseError<TResponse>(Exception exception, string url, object request)
        {
            var webEx = exception as WebException;
            if (PclExportClient.Instance.IsWebException(webEx))
            {
                var errorResponse = (HttpWebResponse)webEx.Response;
                Log.Error(webEx);
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Status Code : {errorResponse.StatusCode}");
                    Log.Debug($"Status Description : {errorResponse.StatusDescription}");
                }

                var serviceEx = new WebServiceException(errorResponse.StatusDescription)
                {
                    StatusCode = (int)errorResponse.StatusCode,
                    StatusDescription = errorResponse.StatusDescription,
                    ResponseHeaders = errorResponse.Headers
                };

                try
                {
                    using var stream = errorResponse.ResponseStream();
                    var bytes = stream.ReadFully();
                    serviceEx.ResponseBody = bytes.FromUtf8Bytes();
                    var errorResponseType = WebRequestUtils.GetErrorResponseDtoType<TResponse>(request);

                    if (stream.CanSeek)
                    {
                        PclExport.Instance.ResetStream(stream);
                        serviceEx.ResponseDto = this.StreamDeserializer(errorResponseType, stream);
                    }
                    else //Android
                    {
                        using var ms = MemoryStreamFactory.GetStream(bytes);
                        serviceEx.ResponseDto = this.StreamDeserializer(errorResponseType, ms);
                    }
                    return serviceEx;
                }
                catch (Exception innerEx)
                {
                    // Oh, well, we tried
                    Log.Debug($"WebException Reading Response Error: {innerEx.Message}", innerEx);
                    return new WebServiceException(errorResponse.StatusDescription, innerEx)
                    {
                        StatusCode = (int)errorResponse.StatusCode,
                        StatusDescription = errorResponse.StatusDescription,
                        ResponseHeaders = errorResponse.Headers
                    };
                }
            }

            if (exception is AuthenticationException authEx)
            {
                var customEx = WebRequestUtils.CreateCustomException(url, authEx);

                Log.Debug($"AuthenticationException: {customEx.Message}", customEx);
                return authEx;
            }

            Log.Debug($"Exception Reading Response Error: {exception.Message}", exception);
            return exception;
        }

        private void HandleAuthException(Exception ex, WebRequest client)
        {
            if (ex is WebException webEx && webEx.Response != null)
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

        public void Dispose() { }
    }
}