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

    public partial class AsyncServiceClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncServiceClient));
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        //private HttpWebRequest webRequest = null;

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
        public Action<WebRequest> OnAuthenticationRequired { get; set; }

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

        public string BaseUri { get; set; }
        public bool DisableAutoCompression { get; set; }

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

        public bool CaptureSynchronizationContext { get; set; }

        public bool HandleCallbackOnUiThread { get; set; }

        public bool EmulateHttpViaPost { get; set; }

        public ProgressDelegate OnDownloadProgress { get; set; }

        public ProgressDelegate OnUploadProgress { get; set; }

        public bool ShareCookiesWithBrowser { get; set; }

        internal Action CancelAsyncFn;

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

        public Task<TResponse> SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request)
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

                SendWebRequest<TResponse>(httpMethod, absoluteUrl, request,
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
                SendWebRequest<TResponse>(httpMethod, absoluteUrl, request,
                    tcs.SetResult,
                    (response, exc) => tcs.SetException(exc)
                );
            }

            return tcs.Task;
        }

        public void SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request,
            Action<TResponse> onSuccess, Action<object, Exception> onError)
        {
            SendWebRequest(httpMethod, absoluteUrl, request, onSuccess, onError);
        }

        private void SendWebRequest<TResponse>(string httpMethod, string absoluteUrl, object request, 
            Action<TResponse> onSuccess, Action<object, Exception> onError, Action<WebResponse> onResponseInit = null)
        {
            if (httpMethod == null) throw new ArgumentNullException("httpMethod");

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
                OnResponseInit = onResponseInit,
                OnSuccess = onSuccess,
                OnError = onError,
                UseSynchronizationContext = CaptureSynchronizationContext ? SynchronizationContext.Current : null,
                HandleCallbackOnUIThread = HandleCallbackOnUiThread,
            };
            requestState.StartTimer(this.Timeout.GetValueOrDefault(DefaultTimeout));

            SendWebRequestAsync(httpMethod, request, requestState, webRequest);
        }

        private void SendWebRequestAsync<TResponse>(string httpMethod, object request,
            AsyncState<TResponse> state, HttpWebRequest webRequest)
        {
            webRequest.Accept = string.Format("{0}, */*", ContentType);

            if (this.EmulateHttpViaPost)
            {
                webRequest.Method = "POST";
                webRequest.Headers[HttpHeaders.XHttpMethodOverride] = httpMethod;
            }
            else
            {
                webRequest.Method = httpMethod;
            }

            PclExportClient.Instance.AddHeader(webRequest, Headers);

            //EmulateHttpViaPost is also forced for SL5 clients sending non GET/POST requests
            PclExport.Instance.Config(webRequest, userAgent: UserAgent);

            if (this.Credentials != null) 
                webRequest.Credentials = this.Credentials;
            if (this.AlwaysSendBasicAuthHeader) 
                webRequest.AddBasicAuth(this.UserName, this.Password);

            ApplyWebRequestFilters(webRequest);

            try
            {
                if (webRequest.Method.HasRequestBody())
                {
                    webRequest.ContentType = ContentType;
                    webRequest.BeginGetRequestStream(RequestCallback<TResponse>, state);
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
                var req = requestState.WebRequest;

                var stream = req.EndGetRequestStream(asyncResult);

                if (requestState.Request != null)
                {
                    StreamSerializer(null, requestState.Request, stream);
                }

                stream.EndWriteStream();

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
                var webRequest = requestState.WebRequest;

                requestState.WebResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

                if (requestState.OnResponseInit != null)
                {
                    requestState.OnResponseInit(requestState.WebResponse);
                }

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
                var responseStream = requestState.WebResponse.GetResponseStream();
                requestState.ResponseStream = responseStream;

                var task = responseStream.ReadAsync(requestState.BufferRead, 0, BufferSize);
                ReadCallBack(task, requestState);
            }
            catch (Exception ex)
            {
                var firstCall = Interlocked.Increment(ref requestState.RequestCount) == 1;
                if (firstCall && WebRequestUtils.ShouldAuthenticate(ex, this.UserName, this.Password))
                {
                    try
                    {
                        requestState.WebRequest = (HttpWebRequest)WebRequest.Create(requestState.Url);

                        if (StoreCookies)
                        {
                            requestState.WebRequest.CookieContainer = CookieContainer;
                        }

                        requestState.WebRequest.AddBasicAuth(this.UserName, this.Password);

                        if (OnAuthenticationRequired != null)
                        {
                            OnAuthenticationRequired(requestState.WebRequest);
                        }

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

                HandleResponseError(ex, requestState);
            }
        }

        private void ReadCallBack<T>(Task<int> task, AsyncState<T> requestState)
        {
            task.ContinueWith(t =>
            {
                try
                {
                    var responseStream = requestState.ResponseStream;

                    int read = t.Result;
                    if (read > 0)
                    {
                        requestState.BytesData.Write(requestState.BufferRead, 0, read);

                        var responeStreamTask = responseStream.ReadAsync(requestState.BufferRead, 0, BufferSize);

                        requestState.ResponseBytesRead += read;
                        if (OnDownloadProgress != null)
                        {
                            OnDownloadProgress(requestState.ResponseBytesRead, requestState.ResponseContentLength);
                        }

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
                        Log.Debug(string.Format("Error Reading Response Error: {0}", ex.Message), ex);
                        requestState.HandleError(default(T), ex);
                    }
                    finally
                    {
                        responseStream.EndReadStream();

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
            if (webEx.IsWebException())
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                Log.Error(webEx);
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
                    Log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);
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
                    Log.Debug(string.Format("WebException Reading Response Error: {0}", innerEx.Message), innerEx);
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

                Log.Debug(string.Format("AuthenticationException: {0}", customEx.Message), customEx);
                state.HandleError(default(TResponse), authEx);
            }

            Log.Debug(string.Format("Exception Reading Response Error: {0}", exception.Message), exception);
            state.HandleError(default(TResponse), exception);

            CancelAsyncFn = null;
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

        public void Dispose() { }
    }
}