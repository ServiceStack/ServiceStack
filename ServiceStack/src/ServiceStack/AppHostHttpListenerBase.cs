#if !NETCORE

using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Host.HttpListener;
using ServiceStack.Text;

namespace ServiceStack;

/// <summary>
/// Inherit from this class if you want to host your web services inside a 
/// Console Application, Windows Service, etc.
/// 
/// Usage of HttpListener allows you to host webservices on the same port (:80) as IIS 
/// however it requires admin user privileges.
/// </summary>
public abstract class AppHostHttpListenerBase
    : HttpListenerBase
{
    public static int ThreadsPerProcessor = 16;

    public static int CalculatePoolSize()
    {
        return Environment.ProcessorCount * ThreadsPerProcessor;
    }

    public string HandlerPath { get; set; }

    protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    { }

    protected AppHostHttpListenerBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    {
        HandlerPath = handlerPath;
    }

    protected override async Task ProcessRequestAsync(HttpListenerContext context)
    {
        if (string.IsNullOrEmpty(context.Request.RawUrl))
            return;
        
        RequestContext.Instance.StartRequestContext();

        var operationName = context.Request.GetOperationName();

        var httpReq = (ListenerRequest)context.ToRequest(operationName);
        var httpRes = httpReq.Response;

        var handler = HttpHandlerFactory.GetHandler(httpReq);

        if (handler is IServiceStackHandler serviceStackHandler)
        {
            var task = serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, httpReq.OperationName);
            await task.ContinueWith(x => httpRes.Close(), 
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent).ConfigAwait();
            //Matches Exceptions handled in HttpListenerBase.InitTask()

            return;
        }

        throw new NotImplementedException($"Cannot execute handler: {handler} at PathInfo: {httpReq.PathInfo}");
    }

    public override void OnConfigLoad()
    {
        base.OnConfigLoad();

        Config.HandlerFactoryPath = string.IsNullOrEmpty(HandlerPath)
            ? null
            : HandlerPath;

        Config.MetadataRedirectPath = string.IsNullOrEmpty(HandlerPath)
            ? "metadata"
            : PathUtils.CombinePaths(HandlerPath, "metadata");
    }
}

#endif
