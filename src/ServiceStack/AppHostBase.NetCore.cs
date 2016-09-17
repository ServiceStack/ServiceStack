#if NETSTANDARD1_6

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
            : base(serviceName, assembliesWithServices) {}

        IApplicationBuilder app;

        public virtual void Bind(IApplicationBuilder app)
        {
            this.app = app;
            LogManager.LogFactory = new NetCoreLogFactory(
                app.ApplicationServices.GetService<ILoggerFactory>());

            Container.Adapter = new NetCoreContainerAdapter(app.ApplicationServices);

            app.Use(ProcessRequest);
        }

        public override void OnConfigLoad()
        {
            //Initialize VFS
            var env = app.ApplicationServices.GetService<IHostingEnvironment>();
            Config.WebHostPhysicalPath = env.WebRootPath ?? env.ContentRootPath;
        }

        public virtual Task ProcessRequest(Microsoft.AspNetCore.Http.HttpContext context, Func<Task> next)
        {
            var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";

            var httpReq = context.ToRequest(operationName);
            var httpRes = httpReq.Response;
            var handler = HttpHandlerFactory.GetHandler(httpReq);

            var serviceStackHandler = handler as IServiceStackHandler;
            if (serviceStackHandler != null)
            {
                if (serviceStackHandler is NotFoundHttpHandler)
                    return next();

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
    }

    public static class NetCoreExtensions
    {
        public static void UseServiceStack(this IApplicationBuilder app, AppHostBase appHost)
        {
            appHost.Bind(app);
            appHost.Init();
        }

        public static IHttpRequest ToRequest(this Microsoft.AspNetCore.Http.HttpContext httpContext, string operationName = null)
        {
            var req = new NetCoreRequest(httpContext, operationName, RequestAttributes.None);
            req.RequestAttributes = req.GetAttributes();
            return req;
        }
    }
}

#endif
