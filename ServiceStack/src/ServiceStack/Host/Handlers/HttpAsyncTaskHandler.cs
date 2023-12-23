//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Host.Handlers;

public abstract class HttpAsyncTaskHandler : IHttpAsyncHandler, IServiceStackHandler
{
    internal static readonly ILog Log = LogManager.GetLogger(typeof(HttpAsyncTaskHandler));

    public string RequestName { get; set; }

    private Type[] ProcessRequestArgTypes = {typeof(IRequest), typeof(IResponse), typeof(string)};

    public virtual bool RunAsAsync()
    {
        var implementsMethod = GetType().GetMethodInfo(nameof(ProcessRequest), ProcessRequestArgTypes);
        return implementsMethod.DeclaringType == typeof(HttpAsyncTaskHandler);
    }

    protected virtual Task CreateProcessRequestTask(IRequest httpReq, IResponse httpRes, string operationName)
    {
#if !NETCORE
        var currentCulture = Thread.CurrentThread.CurrentCulture;
        var currentUiCulture = Thread.CurrentThread.CurrentUICulture;
        var ctx = HttpContext.Current;
#endif

        //preserve Current Culture:
        return new Task(() =>
        {
#if !NETCORE
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
        Log.Error($"HttpAsyncTaskHandler.ProcessRequest() that should never have been called, was just called from: {Environment.StackTrace}");
        ProcessRequestAsync(httpReq, httpRes, operationName).Wait();
    }

    //.NET Core entry point for: 
    // - .NET Core from AppHostBase.ProcessRequest() 
    // - HttpListener from AppHostHttpListenerBase.ProcessRequestAsync()
    public virtual Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
    {
        var task = CreateProcessRequestTask(httpReq, httpRes, operationName);
        task.Start(TaskScheduler.Default);
        return task;
    }

#if !NETCORE
    protected static bool DefaultHandledRequest(HttpListenerContext context) => false;

    protected static bool DefaultHandledRequest(HttpContextBase context) => false;

    //ASP.NET IHttpHandler entry point 
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

    //ASP.NET IHttpAsyncHandler entry point
    IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
    {
        if (cb == null)
            throw new ArgumentNullException(nameof(cb));

        var task = ProcessRequestAsync(context.Request.RequestContext.HttpContext);

        HostContext.Async.ContinueWith(task, ar =>
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

    //Called by both ASP.NET IHttpHandler/IHttpAsyncHandler Requests
    public virtual Task ProcessRequestAsync(HttpContextBase context)
    {
        RequestContext.Instance.StartRequestContext();
        
        var operationName = this.RequestName ?? context.Request.GetOperationName();

        RememberLastRequestInfo(operationName, context.Request.PathInfo);

        if (string.IsNullOrEmpty(operationName)) return TypeConstants.EmptyTask;

        if (DefaultHandledRequest(context)) return TypeConstants.EmptyTask;

        var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(context, operationName);

        if (RunAsAsync())
            return ProcessRequestAsync(httpReq, httpReq.Response, operationName);

        return CreateProcessRequestTask(httpReq, httpReq.Response, operationName);
    }
#else
    public virtual async Task Middleware(Microsoft.AspNetCore.Http.HttpContext context, Func<Task> next)
    {
        var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";

        var httpReq = context.ToRequest(operationName);
        var httpRes = httpReq.Response;

        if (!string.IsNullOrEmpty(RequestName))
            operationName = RequestName;

        try
        {
            var task = ProcessRequestAsync(httpReq, httpRes, operationName);
            await task;
        }
        finally
        {
            await httpRes.CloseAsync();
        }
    }
#endif

    public virtual bool IsReusable => false;

    protected async Task HandleException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
    {
        try
        {
            await HostContext.RaiseAndHandleException(httpReq, httpRes, operationName, ex).ConfigAwait();
        }
        catch (Exception writeErrorEx)
        {
            //Exception in writing to response should not hide the original exception
            Log.Info("Failed to write error to response: {0}", writeErrorEx);
            //rethrow the original exception
            throw ex;
        }
        finally
        {
            await httpRes.EndRequestAsync(skipHeaders: true).ConfigAwait();
        }
    }
}
