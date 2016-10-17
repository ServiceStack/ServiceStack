#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
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
using ServiceStack.IO;
using ServiceStack.VirtualPath;
using System.Linq;
using ServiceStack.Configuration;

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
            var logFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            if (logFactory != null)
            {
                LogManager.LogFactory = new NetCoreLogFactory(logFactory);
            }

            appHost.Container.Adapter = new NetCoreContainerAdapter(app.ApplicationServices);
        }

        /// <summary>
        /// The FilePath used in Virtual File Sources
        /// </summary>
        public override string GetWebRootPath()
        {
            if (app == null)
                return base.GetWebRootPath();

            var env = app.ApplicationServices.GetService<IHostingEnvironment>();
            return env.WebRootPath ?? env.ContentRootPath;
        }

        public override void OnConfigLoad()
        {
            if (app != null)
            {
                //Initialize VFS
                var env = app.ApplicationServices.GetService<IHostingEnvironment>();
                Config.WebHostPhysicalPath = env.ContentRootPath;

                //Set VirtualFiles to point to ContentRootPath (Project Folder)
                VirtualFiles = new FileSystemVirtualPathProvider(this, env.ContentRootPath);
                RegisterLicenseFromAppSettings(AppSettings);
            }
        }

        public static void RegisterLicenseFromAppSettings(IAppSettings appSettings)
        {
            //Automatically register license key stored in <appSettings/>
            var licenceKeyText = appSettings.GetString(NetStandardPclExport.AppSettingsKey);
            if (!string.IsNullOrEmpty(licenceKeyText))
            {
                LicenseUtils.RegisterLicense(licenceKeyText);
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

        public override IRequest TryGetCurrentRequest()
        {
            return GetOrCreateRequest(app.ApplicationServices.GetService<IHttpContextAccessor>());
        }

        /// <summary>
        /// Creates an IRequest from IHttpContextAccessor if it's been registered as a singleton
        /// </summary>
        public static IRequest GetOrCreateRequest(IHttpContextAccessor httpContextAccessor)
        {
            return GetOrCreateRequest(httpContextAccessor?.HttpContext);
        }

        public static IRequest GetOrCreateRequest(HttpContext httpContext)
        {
            if (httpContext != null)
            {
                object oRequest;
                if (httpContext.Items.TryGetValue(Keywords.IRequest, out oRequest))
                    return (IRequest) oRequest;

                var req = httpContext.ToRequest();
                httpContext.Items[Keywords.IRequest] = req;

                return req;
            }
            return null;
        }
    }

    public static class NetCoreAppHostExtensions
    {
        public static IApplicationBuilder UseServiceStack(this IApplicationBuilder app, AppHostBase appHost)
        {
            appHost.Bind(app);
            appHost.Init();
            return app;
        }

        public static IApplicationBuilder Use(this IApplicationBuilder app, System.Web.IHttpAsyncHandler httpHandler)
        {
            return app.Use(httpHandler.Middleware);
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
