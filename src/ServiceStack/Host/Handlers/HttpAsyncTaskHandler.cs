//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Host.AspNet;
using ServiceStack.Host.HttpListener;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public abstract class HttpAsyncTaskHandler : IHttpAsyncHandler, IServiceStackHttpHandler
    {
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

        protected static bool DefaultHandledRequest(HttpContext context)
        {
            return false;
        }

        public virtual bool RunAsAsync()
        {
            return false;
        }

        public virtual Task ProcessRequestAsync(HttpContext context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            if (String.IsNullOrEmpty(operationName)) return EmptyTask;

            if (DefaultHandledRequest(context)) return EmptyTask;

            if (RunAsAsync())
            {
                return ProcessRequestAsync(
                    new AspNetRequest(operationName, context.Request),
                    new AspNetResponse(context.Response),
                    operationName);
            }

            return new Task(() => 
                ProcessRequest(
                    new AspNetRequest(operationName, context.Request),
                    new AspNetResponse(context.Response),
                    operationName));
        }

        public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            throw new NotImplementedException();
        }

        public virtual Task ProcessRequestAsync(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            var task = new Task(() => ProcessRequest(httpReq, httpRes, operationName));            
            return task;
        }

        public virtual void ProcessRequest(HttpContext context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            if (String.IsNullOrEmpty(operationName)) return;

            if (DefaultHandledRequest(context)) return;

            ProcessRequest(
                new AspNetRequest(operationName, context.Request),
                new AspNetResponse(context.Response),
                operationName);
        }

        public virtual void ProcessRequest(HttpListenerContext context)
        {
            var operationName = this.RequestName ?? context.Request.GetOperationName();

            if (String.IsNullOrEmpty(operationName)) return;

            if (DefaultHandledRequest(context)) return;

            ProcessRequest(
                new ListenerRequest(operationName, context.Request),
                new ListenerResponse(context.Response),
                operationName);
        }

        public virtual bool IsReusable
        {
            get { return false; }
        }

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            if (cb == null)
                throw new ArgumentNullException("cb");

            var task = ProcessRequestAsync(context);

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

            task.Dispose();
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            var task = ProcessRequestAsync(context);

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