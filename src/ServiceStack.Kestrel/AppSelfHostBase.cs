using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServiceStack.Logging;
using ServiceStack.NetCore;
using ServiceStack.Text;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract class AppSelfHostBase : ServiceStackHost
    {
        protected AppSelfHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) 
        {
            Platforms.PlatformNetCore.HostInstance = this;
        }

        IApplicationBuilder app;

        public virtual void Bind(IApplicationBuilder app)
        {
            this.app = app;
            AppHostBase.BindHost(this, app);
            app.Use(ProcessRequest);
        }

        public override void OnConfigLoad()
        {
            if (app != null)
            {
                //Initialize VFS
                var env = app.ApplicationServices.GetService<IHostingEnvironment>();
                Config.WebHostPhysicalPath = env.WebRootPath ?? env.ContentRootPath;

                AppHostBase.RegisterLicenseFromAppSettings(AppSettings);

                Config.MetadataRedirectPath = "metadata";
            }
        }

        public virtual Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            //Keep in sync with AppHostBase.NetCore.cs
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

        public override ServiceStackHost Init()
        {
            return this; //Run Init() after Bind()
        }

        protected void RealInit()
        {
            base.Init();
        }

        public override ServiceStackHost Start(string urlBase)
        {
            return Start(new[] { urlBase });
        }

        public IWebHost WebHost { get; private set; }

        public virtual ServiceStackHost Start(string[] urlBases)
        {
            this.WebHost = ConfigureHost(new WebHostBuilder(), urlBases).Build();
            this.WebHost.Start();

            return this;
        }

        public virtual IWebHostBuilder ConfigureHost(IWebHostBuilder host, string[] urlBases)
        {
            return host.UseKestrel()
                .UseContentRoot(System.IO.Directory.GetCurrentDirectory())
                .UseWebRoot(System.IO.Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(urlBases);
        }

        /// <summary>
        /// Override to Configure .NET Core dependencies
        /// </summary>
        public virtual void ConfigureServices(IServiceCollection services) {}

        /// <summary>
        /// Override to Confgiure .NET Core App
        /// </summary>
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env) {}

        public static AppSelfHostBase HostInstance => (AppSelfHostBase)Platforms.PlatformNetCore.HostInstance;

        protected class Startup
        {
            public void ConfigureServices(IServiceCollection services) =>
                HostInstance.ConfigureServices(services);

            public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                HostInstance.Configure(app, env);
                HostInstance.Bind(app);
                HostInstance.RealInit();
            }
        }

        public override IRequest TryGetCurrentRequest()
        {
            return AppHostBase.GetOrCreateRequest(app.ApplicationServices.GetService<IHttpContextAccessor>());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.WebHost?.Dispose();
                this.WebHost = null;
            }

            base.Dispose(disposing);
        }
    }    
}
