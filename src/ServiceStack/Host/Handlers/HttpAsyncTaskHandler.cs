//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Host.AspNet;
using ServiceStack.Host.HttpListener;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public abstract class HttpAsyncTaskHandler : IHttpAsyncHandler, IServiceStackHandler
    {
        internal static readonly ILog Log = LogManager.GetLogger(typeof(HttpAsyncTaskHandler));

        internal static readonly Task<object> EmptyTask;

        static HttpAsyncTaskHandler()
        {
            EmptyTask = ((object)null).AsTaskResult();
        }

        public string RequestName { get; set; }

        protected static bool DefaultHandledRequest(HttpListenerContext context)
        {
            return false;
        }

        protected static bool DefaultHandledRequest(HttpContextBase context)
        {
            return false;
        }

        public virtual bool RunAsAsync()
        {
            return false;
        }

        public virtual Task ProcessRequestAsync(HttpContextBase context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            RememberLastRequestInfo(operationName, context.Request.PathInfo);

            if (String.IsNullOrEmpty(operationName)) return EmptyTask;

            if (DefaultHandledRequest(context)) return EmptyTask;

            var httpReq = new AspNetRequest(context, operationName);

            if (RunAsAsync())
                return ProcessRequestAsync(httpReq, httpReq.Response, operationName);

            return CreateProcessRequestTask(httpReq, httpReq.Response, operationName);
        }

        protected virtual Task CreateProcessRequestTask(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            var currentUiCulture = Thread.CurrentThread.CurrentUICulture;
            var ctx = HttpContext.Current;

            //preserve Current Culture:
            return new Task(() =>
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                Thread.CurrentThread.CurrentUICulture = currentUiCulture;
                //HttpContext is not preserved in ThreadPool threads: http://stackoverflow.com/a/13558065/85785
                if (HttpContext.Current == null)
                    HttpContext.Current = ctx;

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
            task.Start();
            return task;
        }

        public virtual void ProcessRequest(HttpContextBase context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            if (string.IsNullOrEmpty(operationName)) return;

            if (DefaultHandledRequest(context)) return;

            var httpReq = new AspNetRequest(context, operationName);

            ProcessRequest(httpReq, httpReq.Response, operationName);
        }

        public virtual void ProcessRequest(HttpListenerContext context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            RememberLastRequestInfo(operationName, context.Request.RawUrl);

            if (String.IsNullOrEmpty(operationName)) return;

            if (DefaultHandledRequest(context)) return;

            var httpReq = ((HttpListenerBase)ServiceStackHost.Instance).CreateRequest(context, operationName);

            ProcessRequest(httpReq, httpReq.Response, operationName);
        }

        public virtual bool IsReusable
        {
            get { return false; }
        }

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            if (cb == null)
                throw new ArgumentNullException("cb");

            var task = ProcessRequestAsync(context.Request.RequestContext.HttpContext);

            task.ContinueWith(ar =>
                              cb(ar));

            if (task.Status == TaskStatus.Created)
            {
                task.Start();
            }

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

        protected Task HandleException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
            Log.Error(errorMessage, ex);

            try
            {
                HostContext.RaiseAndHandleUncaughtException(httpReq, httpRes, operationName, ex);
                return EmptyTask;
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
    }
}