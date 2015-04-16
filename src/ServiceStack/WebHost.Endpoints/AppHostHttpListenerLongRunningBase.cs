using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Autofac;
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
            private readonly object syncRoot = new object();
            private volatile bool isDisposing;
            private readonly AutoResetEvent autoResetEvent;
            private int avalaibleThreadCount = 0;

            public ThreadPoolManager(int poolSize)
            {
                autoResetEvent = new AutoResetEvent(false);
                avalaibleThreadCount = poolSize;
            }

            public Thread Peek(ThreadStart threadStart)
            {
                while (!isDisposing && avalaibleThreadCount == 0)
                    autoResetEvent.WaitOne();

                lock (syncRoot)
                {
                    if (isDisposing)
                        return null;

                    if (Interlocked.Decrement(ref avalaibleThreadCount) < 0)
                        return Peek(threadStart);
                }

                return new Thread(threadStart);
            }

            public void Free()
            {
                Interlocked.Increment(ref avalaibleThreadCount);
                autoResetEvent.Set();
            }

            /// <summary>
            /// Exécute les tâches définies par l'application associées à la libération ou à la redéfinition des ressources non managées.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
                lock (this)
                {
                    if (isDisposing)
                        return;

                    isDisposing = true;
                }
            }
        }

        private readonly AutoResetEvent listenForNextRequest = new AutoResetEvent(false);
        private readonly ThreadPoolManager threadPoolManager;
        private readonly ILog log = LogManager.GetLogger(typeof(HttpListenerBase));

        protected AppHostHttpListenerLongRunningBase(int poolSize = 500) { threadPoolManager = new ThreadPoolManager(poolSize); }

        protected AppHostHttpListenerLongRunningBase(string serviceName, IContainer dependencyContainer, params Assembly[] assembliesWithServices)
            : this(serviceName, 500, dependencyContainer, assembliesWithServices) { }

        protected AppHostHttpListenerLongRunningBase(string serviceName, int poolSize, IContainer dependencyContainer, params Assembly[] assembliesWithServices)
            : base(serviceName, dependencyContainer, assembliesWithServices) { threadPoolManager = new ThreadPoolManager(poolSize); }

        protected AppHostHttpListenerLongRunningBase(string serviceName, string handlerPath, IContainer dependencyContainer, params Assembly[] assembliesWithServices)
            : this(serviceName, handlerPath, 500, dependencyContainer, assembliesWithServices) { }

        protected AppHostHttpListenerLongRunningBase(string serviceName, string handlerPath, int poolSize, IContainer dependencyContainer, params Assembly[] assembliesWithServices)
            : base(serviceName, handlerPath, dependencyContainer, assembliesWithServices) { threadPoolManager = new ThreadPoolManager(poolSize); }


        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            lock (this)
            {
                if (disposed) return;

                if (disposing)
                {
                    threadPoolManager.Dispose();
                }

                // new shared cleanup logic
                disposed = true;

                base.Dispose(disposing);
            }
        }

        public override void Start(string urlBase)
        {
            Start(urlBase, Listen);
        }

        private bool IsListening
        {
            get { return this.IsStarted && this.Listener != null && this.Listener.IsListening; }
        }

        // Loop here to begin processing of new requests.
        private void Listen(object state)
        {
            while (IsListening)
            {
                if (Listener == null) return;

                try
                {
                    Listener.BeginGetContext(ListenerCallback, Listener);
                    listenForNextRequest.WaitOne();
                }
                catch (Exception ex)
                {
                    log.Error("Listen()", ex);
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
                    log.DebugFormat("Ignoring ListenerCallback() as HttpListener is no longer listening");
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
                log.Warn(errMsg);
                return;
            }
            finally
            {
                // Once we know we have a request (or exception), we signal the other thread
                // so that it calls the BeginGetContext() (or possibly exits if we're not
                // listening any more) method to start handling the next incoming request
                // while we continue to process this request on a different thread.
                listenForNextRequest.Set();
            }

            log.InfoFormat("{0} Request : {1}", context.Request.UserHostAddress, context.Request.RawUrl);

            RaiseReceiveWebRequest(context);


            threadPoolManager.Peek(() =>
            {
                try
                {
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    string error = string.Format("Error this.ProcessRequest(context): [{0}]: {1}", ex.GetType().Name, ex.Message);
                    log.ErrorFormat(error);

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
                        log.ErrorFormat(error);
                    }
                }

                threadPoolManager.Free();
            }).Start();
        }
    }
}
