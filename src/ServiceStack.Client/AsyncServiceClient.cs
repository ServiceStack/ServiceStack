// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

#if NETFX_CORE
using Windows.System.Threading;
#endif

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

        public static int BufferSize = 8192;

        public ICredentials Credentials { get; set; }

        public bool AlwaysSendBasicAuthHeader { get; set; }

        public bool StoreCookies { get; set; }

        public INameValueCollection Headers { get; set; }

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

        public bool CaptureSynchronizationContext { get; set; }

        public bool HandleCallbackOnUiThread { get; set; }

        public bool EmulateHttpViaPost { get; set; }

        public ProgressDelegate OnDownloadProgress { get; set; }

        public ProgressDelegate OnUploadProgress { get; set; }

        public bool ShareCookiesWithBrowser { get; set; }

        public int Version { get; set; }
        public string SessionId { get; set; }

        internal Action CancelAsyncFn;

        public static bool DisableTimer { get; set; }

        public void CancelAsync()
        {
            if (CancelAsyncFn != null)
            {
                // Request will be nulled after it throws an exception on its async methods
                // See - http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.abort
                CancelAsyncFn();
                CancelAsyncFn = null;
            }
        }

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token=default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<TResponse>();

            if (ResultsFilter != null)
            {
                var response = ResultsFilter(typeof(TResponse), httpMethod, absoluteUrl, request);
                if (response is TResponse)
                {
                    tcs.SetResult((TResponse)response);
                    return tcs.Task;
                }
            }

            if (ResultsFilterResponse != null)
            {
                WebResponse webRes = null;

                SendWebRequest<TResponse>(httpMethod, absoluteUrl, request, token,
                    r => {
                        ResultsFilterResponse(webRes, r, httpMethod, absoluteUrl, request);
                        tcs.SetResult(r);
                    },
                    (response, exc) => tcs.SetException(exc),
                    wr => webRes = wr
                );
            }
            else
            {
                SendWebRequest<TResponse>(httpMethod, absoluteUrl, request, token,
                    tcs.SetResult,
                    (response, exc) => tcs.SetException(exc)
                );
            }

            return tcs.Task;
        }

        private void SendWebRequest<TResponse>(string httpMethod, string absoluteUrl, object request, CancellationToken token, 
            Action<TResponse> onSuccess, Action<object, Exception> onError, Action<WebResponse> onResponseInit = null)
        {
            if (httpMethod == null) throw new ArgumentNullException(nameof(httpMethod));

            this.PopulateRequestMetadata(request);

            var requestUri = absoluteUrl;
            var hasQueryString = request != null && !httpMethod.HasRequestBody();
            if (hasQueryString)
            {
                var queryString = QueryStringSerializer.SerializeToString(request);
                if (!string.IsNullOrEmpty(queryString))
                {
                    requestUri += "?" + queryString;
                }
            }

            var webRequest = this.CreateHttpWebRequest(requestUri);

            var requestState = new AsyncState<TResponse>(BufferSize)
            {
                HttpMethod = httpMethod,
                Url = requestUri,
                WebRequest = webRequest,
                Request = request,
                Token = token,
                OnResponseInit = onResponseInit,
                OnSuccess = onSuccess,
                OnError = onError,
                UseSynchronizationContext = CaptureSynchronizationContext ? SynchronizationContext.Current : null,
                HandleCallbackOnUIThread = HandleCallbackOnUiThread,
            };
            if (!DisableTimer)
                requestState.StartTimer(this.Timeout.GetValueOrDefault(DefaultTimeout));

            SendWebRequestAsync(httpMethod, request, requestState, webRequest);
        }

        private void SendWebRequestAsync<TResponse>(string httpMethod, object request,
            AsyncState<TResponse> state, HttpWebRequest client)
        {
            client.Accept = ContentType;

            if (this.EmulateHttpViaPost)
            {
                client.Method = "POST";
                client.Headers[HttpHeaders.XHttpMethodOverride] = httpMethod;
            }
            else
            {
                client.Method = httpMethod;
            }

            PclExportClient.Instance.AddHeader(client, Headers);

            //EmulateHttpViaPost is also forced for SL5 clients sending non GET/POST requests
            PclExport.Instance.Config(client, userAgent: UserAgent);

            if (this.authInfo != null && !string.IsNullOrEmpty(this.UserName))
                client.AddAuthInfo(this.UserName, this.Password, authInfo);
            else if (this.BearerToken != null)
                client.Headers[HttpHeaders.Authorization] = "Bearer " + this.BearerToken;
            else if (this.Credentials != null)
                client.Credentials = this.Credentials;
            else if (this.AlwaysSendBasicAuthHeader)
                client.AddBasicAuth(this.UserName, this.Password);

            ApplyWebRequestFilters(client);

            try
            {
                if (client.Method.HasRequestBody())
                {
                    client.ContentType = ContentType;
                    client.BeginGetRequestStream(RequestCallback<TResponse>, state);
                }
                else
                {
                    state.WebRequest.BeginGetResponse(ResponseCallback<TResponse>, state);
                }
            }
            catch (Exception ex)
            {
                // BeginGetRequestStream can throw if request was aborted
                HandleResponseError(ex, state);
            }
        }

        private void RequestCallback<T>(IAsyncResult asyncResult)
        {
            var requestState = (AsyncState<T>)asyncResult.AsyncState;
            try
            {
                requestState.Token.ThrowIfCancellationRequested();

                var req = requestState.WebRequest;

                var stream = req.EndGetRequestStream(asyncResult);

                if (requestState.Request != null)
                {
                    StreamSerializer(null, requestState.Request, stream);
                }

                PclExportClient.Instance.CloseWriteStream(stream);

                requestState.WebRequest.BeginGetResponse(ResponseCallback<T>, requestState);
            }
            catch (Exception ex)
            {
                HandleResponseError(ex, requestState);
            }
        }

        private void ResponseCallback<T>(IAsyncResult asyncResult)
        {
            var requestState = (AsyncState<T>)asyncResult.AsyncState;
            try
            {
                requestState.Token.ThrowIfCancellationRequested();

                var webRequest = requestState.WebRequest;

                requestState.WebResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

                requestState.OnResponseInit?.Invoke(requestState.WebResponse);

                if (requestState.ResponseContentLength == default(long))
                {
                    requestState.ResponseContentLength = requestState.WebResponse.ContentLength;
                }

                ApplyWebResponseFilters(requestState.WebResponse);

                if (typeof(T) == typeof(HttpWebResponse))
                {
                    requestState.HandleSuccess((T)(object)requestState.WebResponse);
                    return;
                }

                // Read the response into a Stream object.
#if NETSTANDARD1_1 || NETSTANDARD1_6
                var responseStream = requestState.WebResponse.GetResponseStream()
                    .Decompress(requestState.WebResponse.Headers[HttpHeaders.ContentEncoding]);
#else
                var responseStream = requestState.WebResponse.GetResponseStream();
#endif
                requestState.ResponseStream = responseStream;

                var task = responseStream.ReadAsync(requestState.BufferRead, 0, BufferSize);
                ReadCallBack(task, requestState);
            }
            catch (Exception ex)
            {
                var webEx = ex as WebException;
                var firstCall = Interlocked.Increment(ref requestState.RequestCount) == 1;
                if (firstCall && WebRequestUtils.ShouldAuthenticate(webEx,
                    (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                        || Credentials != null
                        || BearerToken != null
                        || OnAuthenticationRequired != null))
                {
                    try
                    {
                        OnAuthenticationRequired?.Invoke();

                        requestState.WebRequest = (HttpWebRequest)WebRequest.Create(requestState.Url);

                        if (StoreCookies)
                            requestState.WebRequest.CookieContainer = CookieContainer;

                        HandleAuthException(ex, requestState.WebRequest);

                        SendWebRequestAsync(
                            requestState.HttpMethod, requestState.Request,
                            requestState, requestState.WebRequest);
                    }
                    catch (Exception /*subEx*/)
                    {
                        HandleResponseError(ex, requestState);
                    }
                    return;
                }

                if (ExceptionFilter != null && webEx != null && webEx.Response != null)
                {
                    var cachedResponse = ExceptionFilter(webEx, webEx.Response, requestState.Url, typeof(T));
                    if (cachedResponse is T)
                    {
                        requestState.OnSuccess((T)cachedResponse);
                        return;
                    }
                }

                HandleResponseError(ex, requestState);
            }
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

        private void ReadCallBack<T>(Task<int> task, AsyncState<T> requestState)
        {
            task.ContinueWith(t =>
            {
                try
                {
                    requestState.Token.ThrowIfCancellationRequested();

                    var responseStream = requestState.ResponseStream;

                    int read = t.Result;
                    if (read > 0)
                    {
                        requestState.BytesData.Write(requestState.BufferRead, 0, read);

                        var responeStreamTask = responseStream.ReadAsync(requestState.BufferRead, 0, BufferSize);

                        requestState.ResponseBytesRead += read;
                        OnDownloadProgress?.Invoke(requestState.ResponseBytesRead, requestState.ResponseContentLength);

                        ReadCallBack(responeStreamTask, requestState);
                        return;
                    }

                    Interlocked.Increment(ref requestState.Completed);

                    var response = default(T);
                    try
                    {
                        requestState.BytesData.Position = 0;
                        if (typeof(T) == typeof(Stream))
                        {
                            response = (T)(object)requestState.BytesData;
                        }
                        else
                        {
                            var reader = requestState.BytesData;
                            try
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    using (var sr = new StreamReader(reader))
                                    {
                                        response = (T)(object)sr.ReadToEnd();
                                    }
                                }
                                else if (typeof(T) == typeof(byte[]))
                                {
                                    response = (T)(object)reader.ToArray();
                                }
                                else
                                {
                                    response = (T)this.StreamDeserializer(typeof(T), reader);
                                }
                            }
                            finally
                            {
                                if (reader.CanRead)
                                    reader.Dispose(); // Not yet disposed, but could've been.
                            }
                        }

                        PclExportClient.Instance.SynchronizeCookies(this);

                        requestState.HandleSuccess(response);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Error Reading Response Error: {ex.Message}", ex);
                        requestState.HandleError(default(T), ex);
                    }
                    finally
                    {
                        PclExportClient.Instance.CloseReadStream(responseStream);

                        CancelAsyncFn = null;
                    }
                }
                catch (Exception ex)
                {
                    HandleResponseError(ex, requestState);
                }
            });
        }

        private void HandleResponseError<TResponse>(Exception exception, AsyncState<TResponse> state)
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
                        var errorResponseType = WebRequestUtils.GetErrorResponseDtoType<TResponse>(state.Request);

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
                        state.HandleError(serviceEx.ResponseDto, serviceEx);
                    }
                }
                catch (Exception innerEx)
                {
                    // Oh, well, we tried
                    Log.Debug($"WebException Reading Response Error: {innerEx.Message}", innerEx);
                    state.HandleError(default(TResponse), new WebServiceException(errorResponse.StatusDescription, innerEx) {
                        StatusCode = (int)errorResponse.StatusCode,
                        StatusDescription = errorResponse.StatusDescription,
                        ResponseHeaders = errorResponse.Headers
                    });
                }
                return;
            }

            var authEx = exception as AuthenticationException;
            if (authEx != null)
            {
                var customEx = WebRequestUtils.CreateCustomException(state.Url, authEx);

                Log.Debug($"AuthenticationException: {customEx.Message}", customEx);
                state.HandleError(default(TResponse), authEx);
            }

            Log.Debug($"Exception Reading Response Error: {exception.Message}", exception);
            state.HandleError(default(TResponse), exception);

            CancelAsyncFn = null;
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