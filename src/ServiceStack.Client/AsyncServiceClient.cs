// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
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

    public partial class AsyncServiceClient : IHasSessionId, IHasVersion
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

        public static int BufferSize = 8192;

        public ICredentials Credentials { get; set; }

        public bool AlwaysSendBasicAuthHeader { get; set; }

        public bool StoreCookies { get; set; }

        public NameValueCollection Headers { get; set; }

        public CookieContainer CookieContainer { get; set; }

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

        public string UserName { get; set; }

        public string Password { get; set; }

        public void SetCredentials(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        public string BearerToken { get; set; }

        public TimeSpan? Timeout { get; set; }

        public string ContentType { get; set; }

        public StreamSerializerDelegate StreamSerializer { get; set; }

        public StreamDeserializerDelegate StreamDeserializer { get; set; }

        public string UserAgent { get; set; }

        public bool EmulateHttpViaPost { get; set; }

        public ProgressDelegate OnDownloadProgress { get; set; }

        public ProgressDelegate OnUploadProgress { get; set; }

        public bool ShareCookiesWithBrowser { get; set; }

        public int Version { get; set; }

        public string SessionId { get; set; }

        public static bool DisableTimer { get; set; }

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

            ApplyWebRequestFilters(webReq);

            try
            {
                if (HttpUtils.HasRequestBody(webReq.Method))
                {
                    webReq.ContentType = ContentType;

                    using (var requestStream = await webReq.GetRequestStreamAsync().ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();
                        if (request != null)
                        {
                            StreamSerializer(null, request, requestStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Error Sending Request: {ex.Message}", ex);

                throw HandleResponseError<T>(ResolveException(ex), requestUri, request);
            }

            try
            {
                webRes = (HttpWebResponse) await webReq.GetResponseAsync().ConfigureAwait(false);
                {
                    token.ThrowIfCancellationRequested();

                    ApplyWebResponseFilters(webRes);

                    returningWebResponse = typeof(T) == typeof(HttpWebResponse);
                    if (returningWebResponse)
                        return Complete((T) (object) webRes);

                    var responseStream = GetResponseStream(webRes);

                    var responseBodyLength = webRes.ContentLength;
                    var bufferRead = new byte[BufferSize];

                    var totalRead = 0;
                    int read;
                    var ms = MemoryStreamFactory.GetStream();

                    while ((read = await responseStream.ReadAsync(bufferRead, 0, bufferRead.Length, token).ConfigureAwait(false)) != 0)
                    {
                        ms.Write(bufferRead, 0, read);
                        totalRead += read;
                        OnDownloadProgress?.Invoke(totalRead, responseBodyLength);
                    }

                    try
                    {
                        ms.Position = 0;
                        if (typeof(T) == typeof(Stream))
                        {
                            return Complete((T) (object) ms);
                        }
                        else
                        {
                            var reader = ms;
                            try
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    using (var sr = new StreamReader(reader))
                                    {
                                        return Complete((T) (object) sr.ReadToEnd());
                                    }
                                }
                                else if (typeof(T) == typeof(byte[]))
                                    return Complete((T) (object) reader.ToArray());
                                else
                                    return Complete((T) this.StreamDeserializer(typeof(T), reader));
                            }
                            finally
                            {
                                if (reader.CanRead)
                                    reader.Dispose(); // Not yet disposed, but could've been.
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
                        responseStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var webEx = ex as WebException;
                var firstCall = !recall;
                if (firstCall && WebRequestUtils.ShouldAuthenticate(webEx,
                        (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                        || Credentials != null
                        || BearerToken != null
                        || RefreshToken != null
                        || OnAuthenticationRequired != null))
                {
                    try
                    {
                        if (RefreshToken != null)
                        {
                            var refreshRequest = new GetAccessToken {RefreshToken = RefreshToken};
                            var uri = this.RefreshTokenUri ??
                                      this.BaseUri.CombineWith(refreshRequest.ToPostUrl());

                            GetAccessTokenResponse tokenResponse;
                            try
                            {
                                tokenResponse = uri.PostJsonToUrl(refreshRequest)
                                    .FromJson<GetAccessTokenResponse>();
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
                            if (string.IsNullOrEmpty(accessToken))
                                throw new RefreshTokenException("Could not retrieve new AccessToken from: " + uri);

                            var refreshClient = webReq = (HttpWebRequest) WebRequest.Create(requestUri);
                            if (this.CookieContainer.GetTokenCookie(BaseUri) != null)
                            {
                                this.CookieContainer.SetTokenCookie(accessToken, BaseUri);
                                refreshClient.CookieContainer.SetTokenCookie(BaseUri, accessToken);
                            }
                            else
                            {
                                refreshClient.AddBearerToken(this.BearerToken = accessToken);
                            }

                            return await SendWebRequestAsync<T>(httpMethod, absoluteUrl, request, token, recall: true).ConfigureAwait(false);
                        }

                        OnAuthenticationRequired?.Invoke();

                        var newReq = (HttpWebRequest) WebRequest.Create(requestUri);

                        if (StoreCookies)
                            newReq.CookieContainer = CookieContainer;

                        HandleAuthException(ex, webReq);

                        return await SendWebRequestAsync<T>(httpMethod, absoluteUrl, request, token, recall: true).ConfigureAwait(false);
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

        private static Stream GetResponseStream(WebResponse webRes)
        {
#if NETSTANDARD2_0
            return webRes.GetResponseStream().Decompress(webRes.Headers[HttpHeaders.ContentEncoding]);
#else
            return webRes.GetResponseStream();
#endif
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
                    using (var stream = errorResponse.GetResponseStream())
                    {
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
                            using (var ms = MemoryStreamFactory.GetStream(bytes))
                            {
                                serviceEx.ResponseDto = this.StreamDeserializer(errorResponseType, ms);
                            }
                        }
                        return serviceEx;
                    }
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