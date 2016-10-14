//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public abstract class HttpAsyncTaskHandler : IHttpAsyncHandler, IServiceStackHandler
    {
        internal static readonly ILog Log = LogManager.GetLogger(typeof(HttpAsyncTaskHandler));

        public string RequestName { get; set; }

        public virtual bool RunAsAsync() => false;

#if !NETSTANDARD1_6
        protected static bool DefaultHandledRequest(HttpListenerContext context) => false;

        protected static bool DefaultHandledRequest(HttpContextBase context) => false;

        public virtual Task ProcessRequestAsync(HttpContextBase context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            RememberLastRequestInfo(operationName, context.Request.PathInfo);

            if (string.IsNullOrEmpty(operationName)) return TypeConstants.EmptyTask;

            if (DefaultHandledRequest(context)) return TypeConstants.EmptyTask;

            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(context, operationName);

            if (RunAsAsync())
                return ProcessRequestAsync(httpReq, httpReq.Response, operationName);

            return CreateProcessRequestTask(httpReq, httpReq.Response, operationName);
        }
#endif

        protected virtual Task CreateProcessRequestTask(IRequest httpReq, IResponse httpRes, string operationName)
        {
#if !NETSTANDARD1_6
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            var currentUiCulture = Thread.CurrentThread.CurrentUICulture;
            var ctx = HttpContext.Current;
#endif

            //preserve Current Culture:
            return new Task(() =>
            {
#if !NETSTANDARD1_6
                Thread.CurrentThread.CurrentCulture = currentCulture;
                Thread.CurrentThread.CurrentUICulture = currentUiCulture;
                //HttpContext is not preserved in ThreadPool threads: http://stackoverflow.com/a/13558065/85785
                if (HttpContext.Current == null)
                    HttpContext.Current = ctx;
#endif

                ProcessRequest(httpReq, httpRes, operationName);
            });
        }

        private void RememberLastRequestInfo(string operationName, string pathInfo)
        {
            if (HostContext.DebugMode)
            {
                RequestInfoHandler.LastRequestInfo = new RequestHandlerInfo
                {
                    HandlerType = GetType().Name,
                    OperationName = operationName,
                    PathInfo = pathInfo,
                };
            }
        }

        public virtual void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            throw new NotImplementedException();
        }

        public virtual Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var task = CreateProcessRequestTask(httpReq, httpRes, operationName);
            task.Start(TaskScheduler.Default);
            return task;
        }

#if !NETSTANDARD1_6
        public virtual void ProcessRequest(HttpContextBase context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            if (string.IsNullOrEmpty(operationName)) return;

            if (DefaultHandledRequest(context)) return;

            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(context, operationName);

            ProcessRequest(httpReq, httpReq.Response, operationName);
        }

        public virtual void ProcessRequest(HttpListenerContext context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            RememberLastRequestInfo(operationName, context.Request.RawUrl);

            if (string.IsNullOrEmpty(operationName)) return;

            if (DefaultHandledRequest(context)) return;

            var httpReq = ((HttpListener.HttpListenerBase)ServiceStackHost.Instance).CreateRequest(context, operationName);

            ProcessRequest(httpReq, httpReq.Response, operationName);
        }

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            if (cb == null)
                throw new ArgumentNullException(nameof(cb));

            var task = ProcessRequestAsync(context.Request.RequestContext.HttpContext);

            task.ContinueWith(ar =>
                cb(ar));

            if (task.Status == TaskStatus.Created)
                task.Start(TaskScheduler.Default);

            return task;
        }

        void IHttpAsyncHandler.EndProcessRequest(IAsyncResult result)
        {
            var task = (Task)result;

            task.Wait(); // avoid an exception being thrown on the finalizer thread.

            // Shouldn't dispose of tasks:
            // http://blogs.msdn.com/b/pfxteam/archive/2012/03/25/10287435.aspx
            // http://bradwilson.typepad.com/blog/2012/04/tpl-and-servers-pt4.html
            //task.Dispose();
        }
#else
        public virtual Task Middleware(Microsoft.AspNetCore.Http.HttpContext context, Func<Task> next)
        {
            var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";

            var httpReq = context.ToRequest(operationName);
            var httpRes = httpReq.Response;

            if (!string.IsNullOrEmpty(RequestName))
                operationName = RequestName;

            var task = ProcessRequestAsync(httpReq, httpRes, operationName);
            task.ContinueWith(x => httpRes.Close(), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
            return task;
        }
#endif

        public virtual bool IsReusable => false;

        protected Task HandleException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            var errorMessage = $"Error occured while Processing Request: {ex.Message}";
            HostContext.AppHost.OnLogError(typeof(HttpAsyncTaskHandler), errorMessage, ex);

            try
            {
                HostContext.RaiseAndHandleUncaughtException(httpReq, httpRes, operationName, ex);
                return TypeConstants.EmptyTask;
            }
            catch (Exception writeErrorEx)
            {
                //Exception in writing to response should not hide the original exception
                Log.Info("Failed to write error to response: {0}", writeErrorEx);
                //rethrow the original exception
                return ex.AsTaskException();
            }
            finally
            {
                httpRes.EndRequest(skipHeaders: true);
            }
        }

#if !NETSTANDARD1_6
        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            var task = ProcessRequestAsync(context.Request.RequestContext.HttpContext);

            if (task.Status == TaskStatus.Created)
            {
                task.RunSynchronously();
            }
            else
            {
                task.Wait();
            }
        }
#endif

    }
}