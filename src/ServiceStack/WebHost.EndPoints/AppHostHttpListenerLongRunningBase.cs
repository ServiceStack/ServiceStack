using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{




    public abstract class AppHostHttpListenerLongRunningBase : AppHostHttpListenerBase
    {
        private class ThreadPoolManager : IDisposable
        {
            private readonly object _syncRoot = new object();
            private volatile bool _isDisposing;
            private readonly AutoResetEvent _autoResetEvent;
            private int _avalaibleThreadCount = 0;

            public ThreadPoolManager(int poolSize)
            {
                _autoResetEvent = new AutoResetEvent(false);
                _avalaibleThreadCount = poolSize;
            }

            public Thread Peek(ThreadStart threadStart)
            {
                while (!_isDisposing && _avalaibleThreadCount == 0)
                    _autoResetEvent.WaitOne();

                lock (_syncRoot)
                {
                    if (_isDisposing)
                        return null;

                    if (Interlocked.Decrement(ref _avalaibleThreadCount) < 0)
                        return Peek(threadStart);
                }

                return new Thread(threadStart);
            }

            public void Free()
            {
                Interlocked.Increment(ref _avalaibleThreadCount);
                _autoResetEvent.Set();
            }

            /// <summary>
            /// Exécute les tâches définies par l'application associées à la libération ou à la redéfinition des ressources non managées.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
                lock (this)
                {
                    if (_isDisposing)
                        return;

                    _isDisposing = true;
                }
            }
        }

        private readonly AutoResetEvent _listenForNextRequest = new AutoResetEvent(false);
        private readonly ThreadPoolManager _threadPoolManager;
        private readonly ILog _log = LogManager.GetLogger(typeof(HttpListenerBase));


        protected AppHostHttpListenerLongRunningBase(int poolSize = 500) { _threadPoolManager = new ThreadPoolManager(poolSize); }

        protected AppHostHttpListenerLongRunningBase(string serviceName, params Assembly[] assembliesWithServices)
            : this(serviceName, 500, assembliesWithServices) { }

        protected AppHostHttpListenerLongRunningBase(string serviceName, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) { _threadPoolManager = new ThreadPoolManager(poolSize); }

        protected AppHostHttpListenerLongRunningBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : this(serviceName, handlerPath, 500, assembliesWithServices) {}

        protected AppHostHttpListenerLongRunningBase(string serviceName, string handlerPath, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, handlerPath, assembliesWithServices) { _threadPoolManager = new ThreadPoolManager(poolSize); }


        #region IDisposable Members

        public override void Dispose()
        {
            base.Dispose();
            _threadPoolManager.Dispose();

            Instance = null;
        }

        #endregion

        /// <summary>
        /// Starts the Web Service
        /// </summary>
        /// <param name="urlBase">
        /// A Uri that acts as the base that the server is listening on.
        /// Format should be: http://127.0.0.1:8080/ or http://127.0.0.1:8080/somevirtual/
        /// Note: the trailing backslash is required! For more info see the
        /// HttpListener.Prefixes property on MSDN.
        /// </param>
        public override void Start(string urlBase)
        {
            // *** Already running - just leave it in place
            if (IsStarted)
                return;

            if (Listener == null)
            {
                Listener = new HttpListener();
            }

            Listener.Prefixes.Add(urlBase);

            IsStarted = true;
            Listener.Start();

            ThreadPool.QueueUserWorkItem(Listen);
        }

        // Loop here to begin processing of new requests.
        private void Listen(object state)
        {
            while (Listener.IsListening)
            {
                if (Listener == null) return;

                try
                {
                    Listener.BeginGetContext(ListenerCallback, Listener);
                    _listenForNextRequest.WaitOne();
                }
                catch (Exception ex)
                {
                    _log.Error("Listen()", ex);
                    return;
                }
                if (Listener == null) return;
            }
        }

        // Handle the processing of a request in here.
        private void ListenerCallback(IAsyncResult asyncResult)
        {
            var listener = asyncResult.AsyncState as HttpListener;
            HttpListenerContext context;

            if (listener == null) return;
            var isListening = listener.IsListening;

            try
            {
                if (!isListening)
                {
                    _log.DebugFormat("Ignoring ListenerCallback() as HttpListener is no longer listening");
                    return;
                }
                // The EndGetContext() method, as with all Begin/End asynchronous methods in the .NET Framework,
                // blocks until there is a request to be processed or some type of data is available.
                context = listener.EndGetContext(asyncResult);
            }
            catch (Exception ex)
            {
                // You will get an exception when httpListener.Stop() is called
                // because there will be a thread stopped waiting on the .EndGetContext()
                // method, and again, that is just the way most Begin/End asynchronous
                // methods of the .NET Framework work.
                string errMsg = ex + ": " + isListening;
                _log.Warn(errMsg);
                return;
            }
            finally
            {
                // Once we know we have a request (or exception), we signal the other thread
                // so that it calls the BeginGetContext() (or possibly exits if we're not
                // listening any more) method to start handling the next incoming request
                // while we continue to process this request on a different thread.
                _listenForNextRequest.Set();
            }

            _log.InfoFormat("{0} Request : {1}", context.Request.UserHostAddress, context.Request.RawUrl);

            RaiseReceiveWebRequest(context);


            _threadPoolManager.Peek(() =>
                           {
                               try
                               {
                                   ProcessRequest(context);
                               }
                               catch (Exception ex)
                               {
                                   string error = string.Format("Error this.ProcessRequest(context): [{0}]: {1}", ex.GetType().Name, ex.Message);
                                   _log.ErrorFormat(error);

                                   try
                                   {
                                       var sb = new StringBuilder();
                                       sb.AppendLine("{");
                                       sb.AppendLine("\"ResponseStatus\":{");
                                       sb.AppendFormat(" \"ErrorCode\":{0},\n", ex.GetType().Name.EncodeJson());
                                       sb.AppendFormat(" \"Message\":{0},\n", ex.Message.EncodeJson());
                                       sb.AppendFormat(" \"StackTrace\":{0}\n", ex.StackTrace.EncodeJson());
                                       sb.AppendLine("}");
                                       sb.AppendLine("}");

                                       context.Response.StatusCode = 500;
                                       context.Response.ContentType = ContentType.Json;
                                       byte[] sbBytes = sb.ToString().ToUtf8Bytes();
                                       context.Response.OutputStream.Write(sbBytes, 0, sbBytes.Length);
                                       context.Response.Close();
                                   }
                                   catch (Exception errorEx)
                                   {
                                       error = string.Format("Error this.ProcessRequest(context)(Exception while writing error to the response): [{0}]: {1}",
                                                             errorEx.GetType().Name, errorEx.Message);
                                       _log.ErrorFormat(error);
                                   }
                               }

                               _threadPoolManager.Free();
                           }).Start();
        }
    }
}