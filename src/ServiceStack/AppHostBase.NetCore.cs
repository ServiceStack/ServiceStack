﻿#if NETSTANDARD1_6

using System;
using System.Reflection;
using System.Threading.Tasks;

using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Logging;
using ServiceStack.NetCore;
using ServiceStack.Host;
using ServiceStack.Host.NetCore;
using ServiceStack.Host.Handlers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack
{
    public abstract class AppHostBase : ServiceStackHost
    {
        protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) 
        {
            Platforms.PlatformNetCore.HostInstance = this;
        }

        IApplicationBuilder app;

        public virtual void Bind(IApplicationBuilder app)
        {
            this.app = app;
            BindHost(this, app);
            app.Use(ProcessRequest);
        }

        public static void BindHost(ServiceStackHost appHost, IApplicationBuilder app)
        {
            JsConfig.EmitCamelCaseNames = true;

            var logFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            if (logFactory != null)
            {
                LogManager.LogFactory = new NetCoreLogFactory(logFactory);
            }

            appHost.Container.Adapter = new NetCoreContainerAdapter(app.ApplicationServices);
        }

        public override void OnConfigLoad()
        {
            if (app != null)
            {
                //Initialize VFS
                var env = app.ApplicationServices.GetService<IHostingEnvironment>();
                Config.WebHostPhysicalPath = env.WebRootPath ?? env.ContentRootPath;
            }
        }

        public virtual Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            //Keep in sync with Kestrel/AppSelfHostBase.cs
            var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";

            var httpReq = context.ToRequest(operationName);
            var httpRes = httpReq.Response;
            var handler = HttpHandlerFactory.GetHandler(httpReq);

            var serviceStackHandler = handler as IServiceStackHandler;
            if (serviceStackHandler != null)
            {
                if (serviceStackHandler is NotFoundHttpHandler)
                    return next();

                if (!string.IsNullOrEmpty(serviceStackHandler.RequestName))
                    operationName = serviceStackHandler.RequestName;

                var restHandler = serviceStackHandler as RestHandler;
                if (restHandler != null)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.GetOperationName();
                }

                var task = serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, operationName);
                task.ContinueWith(x => httpRes.Close(), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
                //Matches Exceptions handled in HttpListenerBase.InitTask()

                return task;
            }

            return next();
        }
        public override string MapProjectPath(string relativePath)
        {
            return relativePath.MapHostAbsolutePath();
        }
    }

    public static class NetCoreAppHostExtensions
    {
        public static void UseServiceStack(this IApplicationBuilder app, AppHostBase appHost)
        {
            appHost.Bind(app);
            appHost.Init();
        }

        public static IHttpRequest ToRequest(this HttpContext httpContext, string operationName = null)
        {
            var req = new NetCoreRequest(httpContext, operationName, RequestAttributes.None);
            req.RequestAttributes = req.GetAttributes();
            return req;
        }
    }
}

#endif
