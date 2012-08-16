using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.Common.Web;

namespace ServiceStack.ServiceClient.Web
{
    /**
     * Need to provide async request options
     * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
     */

    public class AsyncServiceClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncServiceClient));
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        private HttpWebRequest _webRequest = null;

        /// <summary>
        /// The request filter is called before any request.
        /// This request filter is executed globally.
        /// </summary>
        public static Action<HttpWebRequest> HttpWebRequestFilter { get; set; }

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// This response action is executed globally.
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        public static Action<HttpWebResponse> HttpWebResponseFilter { get; set; }

        /// <summary>
        /// Called before request resend, when the initial request required authentication
        /// </summary>
        public Action<WebRequest> OnAuthenticationRequired { get; set; }

        const int BufferSize = 4096;

        public ICredentials Credentials { get; set; }

        public bool StoreCookies { get; set; }

        public CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// The request filter is called before any request.
        /// This request filter only works with the instance where it was set (not global).
        /// </summary>
        public Action<HttpWebRequest> LocalHttpWebRequestFilter { get; set; }

        /// <summary>
        /// The response action is called once the server response is available.
        /// It will allow you to access raw response information. 
        /// Note that you should NOT consume the response stream as this is handled by ServiceStack
        /// </summary>
        public Action<HttpWebResponse> LocalHttpWebResponseFilter { get; set; }

        public string BaseUri { get; set; }

        internal class RequestState<TResponse> : IDisposable
        {
            private bool _timedOut; // Pass the correct error back even on Async Calls

            public RequestState()
            {
                BufferRead = new byte[BufferSize];
                TextData = new StringBuilder();
                BytesData = new MemoryStream(BufferSize);
                WebRequest = null;
                ResponseStream = null;
            }

            public string HttpMethod;

            public string Url;

            public StringBuilder TextData;

            public MemoryStream BytesData;

            public byte[] BufferRead;

            public object Request;

            public HttpWebRequest WebRequest;

            public HttpWebResponse WebResponse;

            public Stream ResponseStream;

            public int Completed;

            public int RequestCount;

            public Timer Timer;

            public Action<TResponse> OnSuccess;

            public Action<TResponse, Exception> OnError;

#if SILVERLIGHT
            public bool HandleCallbackOnUIThread { get; set; }
#endif

            public void HandleSuccess(TResponse response)
            {
                if (this.OnSuccess == null)
                    return;

#if SILVERLIGHT
                if (this.HandleCallbackOnUIThread)
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => this.OnSuccess(response));
                else
                    this.OnSuccess(response);
#else
                this.OnSuccess(response);
#endif
            }
            
            public void HandleError(TResponse response, Exception ex)
            {
                if (this.OnError == null)
                    return;

                Exception toReturn = ex;
                if (_timedOut)
                {
#if SILVERLIGHT
                    WebException we = new WebException("The request timed out", ex, WebExceptionStatus.RequestCanceled, null);
#else
                    WebException we = new WebException("The request timed out", ex, WebExceptionStatus.Timeout, null);
#endif
                    toReturn = we;
                }

#if SILVERLIGHT
                if (this.HandleCallbackOnUIThread)
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => this.OnError(response, toReturn));
                else
                    this.OnError(response, toReturn);
#else
                OnError(response, toReturn);
#endif
            }

            public void StartTimer(TimeSpan timeOut)
            {
                this.Timer = new Timer(this.TimedOut, this, (int)timeOut.TotalMilliseconds, System.Threading.Timeout.Infinite);
            }

            public void TimedOut(object state)
            {
                if (Interlocked.Increment(ref Completed) == 1)
                {
                    if (this.WebRequest != null)
                    {
                        _timedOut = true;
                        this.WebRequest.Abort();
                    }
                }
                this.Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                this.Timer.Dispose();
                this.Dispose();
            }

            public void Dispose()
            {
                if (this.BytesData == null) return;
                this.BytesData.Dispose();
                this.BytesData = null;
            }
        }

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

#if SILVERLIGHT
        public bool HandleCallbackOnUIThread { get; set; }

        public bool UseBrowserHttpHandling { get; set; }

        public bool ShareCookiesWithBrowser { get; set; }
#endif

        public void SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request,
            Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            SendWebRequest(httpMethod, absoluteUrl, request, onSuccess, onError);
        }

        public void CancelAsync()
        {
            if (_webRequest != null)
            {
                // Request will be nulled after it throws an exception on its async methods
                // See - http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.abort
                _webRequest.Abort();
            }
        }

#if !SILVERLIGHT
        internal static void AllowAutoCompression(HttpWebRequest webRequest)
        {
            webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;            
        }
#endif

        private RequestState<TResponse> SendWebRequest<TResponse>(string httpMethod, string absoluteUrl, object request, 
            Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
        {
            if (httpMethod == null) throw new ArgumentNullException("httpMethod");

            var requestUri = absoluteUrl;
            var httpGetOrDelete = (httpMethod == "GET" || httpMethod == "DELETE");
            var hasQueryString = request != null && httpGetOrDelete;
            if (hasQueryString)
            {
                var queryString = QueryStringSerializer.SerializeToString(request);
                if (!string.IsNullOrEmpty(queryString))
                {
                    requestUri += "?" + queryString;
                }
            }

#if SILVERLIGHT

            var creator = this.UseBrowserHttpHandling
                            ? System.Net.Browser.WebRequestCreator.BrowserHttp
                            : System.Net.Browser.WebRequestCreator.ClientHttp;

            var webRequest = (HttpWebRequest) creator.Create(new Uri(requestUri));

            if (StoreCookies && !UseBrowserHttpHandling)
            {
                if (ShareCookiesWithBrowser)
                {
                    if (CookieContainer == null)
                        CookieContainer = new CookieContainer();
                    CookieContainer.SetCookies(new Uri(BaseUri), System.Windows.Browser.HtmlPage.Document.Cookies);
                }
                
                webRequest.CookieContainer = CookieContainer;	
            }

#else
            _webRequest = (HttpWebRequest)WebRequest.Create(requestUri);

            if (StoreCookies)
            {
                _webRequest.CookieContainer = CookieContainer;
            }
#endif

#if !SILVERLIGHT
            if (!DisableAutoCompression)
            {
                _webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                _webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;                
            }
#endif

            var requestState = new RequestState<TResponse>
            {
                HttpMethod = httpMethod,
                Url = requestUri,
#if SILVERLIGHT
                WebRequest = webRequest,
#else
                WebRequest = _webRequest,
#endif
                Request = request,
                OnSuccess = onSuccess,
                OnError = onError,
#if SILVERLIGHT
                HandleCallbackOnUIThread = HandleCallbackOnUIThread,
#endif
            };
            requestState.StartTimer(this.Timeout.GetValueOrDefault(DefaultTimeout));

#if SILVERLIGHT
            SendWebRequestAsync(httpMethod, request, requestState, webRequest);
#else
            SendWebRequestAsync(httpMethod, request, requestState, _webRequest);
#endif

            return requestState;
        }

        private void SendWebRequestAsync<TResponse>(string httpMethod, object request, 
            RequestState<TResponse> requestState, HttpWebRequest webRequest)
        {
            var httpGetOrDelete = (httpMethod == "GET" || httpMethod == "DELETE");
            webRequest.Accept = string.Format("{0}, */*", ContentType);

#if !SILVERLIGHT 
            webRequest.Method = httpMethod;
#else
            //Methods others than GET and POST are only supported by Client request creator, see
            //http://msdn.microsoft.com/en-us/library/cc838250(v=vs.95).aspx
            
            if (this.UseBrowserHttpHandling && httpMethod != "GET" && httpMethod != "POST") 
            {
                webRequest.Method = "POST"; 
                webRequest.Headers[HttpHeaders.XHttpMethodOverride] = httpMethod;
            }
            else
            {
                webRequest.Method = httpMethod;
            }
#endif

            if (this.Credentials != null)
            {
                webRequest.Credentials = this.Credentials;
            }

            ApplyWebRequestFilters(webRequest);

            try
            {
                if (!httpGetOrDelete && request != null)
                {
                    webRequest.ContentType = ContentType;
                    webRequest.BeginGetRequestStream(RequestCallback<TResponse>, requestState);
                }
                else
                {
                    requestState.WebRequest.BeginGetResponse(ResponseCallback<TResponse>, requestState);
                }
            }
            catch (Exception ex)
            {
                // BeginGetRequestStream can throw if request was aborted
                HandleResponseError(ex, requestState);
            }
        }

        private void RequestCallback<T>(IAsyncResult asyncResult)
        {
            var requestState = (RequestState<T>)asyncResult.AsyncState;
            try
            {
                var req = requestState.WebRequest;

                var postStream = req.EndGetRequestStream(asyncResult);
                StreamSerializer(null, requestState.Request, postStream);
                postStream.Close();
                requestState.WebRequest.BeginGetResponse(ResponseCallback<T>, requestState);
            }
            catch (Exception ex)
            {
                HandleResponseError(ex, requestState);
            }
        }

        private void ResponseCallback<T>(IAsyncResult asyncResult)
        {
            var requestState = (RequestState<T>)asyncResult.AsyncState;
            try
            {
                var webRequest = requestState.WebRequest;

                requestState.WebResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

                ApplyWebResponseFilters(requestState.WebResponse);

                // Read the response into a Stream object.
                var responseStream = requestState.WebResponse.GetResponseStream();
                requestState.ResponseStream = responseStream;

                responseStream.BeginRead(requestState.BufferRead, 0, BufferSize, ReadCallBack<T>, requestState);
                return;
            }
            catch (Exception ex)
            {
                var firstCall = Interlocked.Increment(ref requestState.RequestCount) == 1;
                if (firstCall && WebRequestUtils.ShouldAuthenticate(ex, this.UserName, this.Password))
                {
                    try
                    {
                        requestState.WebRequest = (HttpWebRequest)WebRequest.Create(requestState.Url);

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

        private void ReadCallBack<T>(IAsyncResult asyncResult)
        {
            var requestState = (RequestState<T>)asyncResult.AsyncState;
            try
            {
                var responseStream = requestState.ResponseStream;
                int read = responseStream.EndRead(asyncResult);

                if (read > 0)
                {
                    requestState.BytesData.Write(requestState.BufferRead, 0, read);
                    responseStream.BeginRead(
                        requestState.BufferRead, 0, BufferSize, ReadCallBack<T>, requestState);

                    return;
                }

                Interlocked.Increment(ref requestState.Completed);

                var response = default(T);
                try
                {
                    requestState.BytesData.Position = 0;
                    using (var reader = requestState.BytesData)
                    {
                        response = (T)this.StreamDeserializer(typeof(T), reader);
                    }

#if SILVERLIGHT
                    if (this.StoreCookies && this.ShareCookiesWithBrowser && !this.UseBrowserHttpHandling)
                    {
                        // browser cookies must be set on the ui thread
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                            () =>
                                {
                                    var cookieHeader = this.CookieContainer.GetCookieHeader(new Uri(BaseUri));
                                    System.Windows.Browser.HtmlPage.Document.Cookies = cookieHeader;
                                });
                    }
#endif

                    requestState.HandleSuccess(response);
                }
                catch (Exception ex)
                {
                    Log.Debug(string.Format("Error Reading Response Error: {0}", ex.Message), ex);
                    requestState.HandleError(default(T), ex);
                }
                finally
                {
                    responseStream.Close();
                    _webRequest = null;
                }
            }
            catch (Exception ex)
            {
                HandleResponseError(ex, requestState);
            }
        }

        private void HandleResponseError<TResponse>(Exception exception, RequestState<TResponse> requestState)
        {
            var webEx = exception as WebException;
            if (webEx != null
#if !SILVERLIGHT 
                && webEx.Status == WebExceptionStatus.ProtocolError
#endif
            )
            {
                var errorResponse = ((HttpWebResponse)webEx.Response);
                Log.Error(webEx);
                Log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
                Log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);

                var serviceEx = new WebServiceException(errorResponse.StatusDescription)
                {
                    StatusCode = (int)errorResponse.StatusCode,
                };

                try
                {
                    using (var stream = errorResponse.GetResponseStream())
                    {
                        //Uncomment to Debug exceptions:
                        //var strResponse = new StreamReader(stream).ReadToEnd();
                        //Console.WriteLine("Response: " + strResponse);
                        //stream.Position = 0;

                        serviceEx.ResponseDto = this.StreamDeserializer(typeof(TResponse), stream);
                        requestState.HandleError((TResponse)serviceEx.ResponseDto, serviceEx);
                    }
                }
                catch (Exception innerEx)
                {
                    // Oh, well, we tried
                    Log.Debug(string.Format("WebException Reading Response Error: {0}", innerEx.Message), innerEx);
                    requestState.HandleError(default(TResponse), new WebServiceException(errorResponse.StatusDescription, innerEx)
                        {
                            StatusCode = (int)errorResponse.StatusCode,
                        });
                }
                return;
            }

            var authEx = exception as AuthenticationException;
            if (authEx != null)
            {
                var customEx = WebRequestUtils.CreateCustomException(requestState.Url, authEx);

                Log.Debug(string.Format("AuthenticationException: {0}", customEx.Message), customEx);
                requestState.HandleError(default(TResponse), authEx);
            }

            Log.Debug(string.Format("Exception Reading Response Error: {0}", exception.Message), exception);
            requestState.HandleError(default(TResponse), exception);

            _webRequest = null;
        }

        private void ApplyWebResponseFilters(WebResponse webResponse)
        {
            if (!(webResponse is HttpWebResponse)) return;

            if (HttpWebResponseFilter != null)
                HttpWebResponseFilter((HttpWebResponse)webResponse);
            if (LocalHttpWebResponseFilter != null)
                LocalHttpWebResponseFilter((HttpWebResponse)webResponse);
        }

        private void ApplyWebRequestFilters(HttpWebRequest client)
        {
            if (LocalHttpWebRequestFilter != null)
                LocalHttpWebRequestFilter(client);

            if (HttpWebRequestFilter != null)
                HttpWebRequestFilter(client);
        }

        public void Dispose() { }
    }

}