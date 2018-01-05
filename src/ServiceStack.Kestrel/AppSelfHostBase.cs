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
using ServiceStack.Host.NetCore;
using ServiceStack.IO;
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

            if (pathBase != null)
            {
                this.app.UsePathBase(pathBase);
            }

            AppHostBase.BindHost(this, app);
            app.Use(ProcessRequest);
        }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();
            if (app != null)
            {
                //Initialize VFS
                var env = app.ApplicationServices.GetService<IHostingEnvironment>();
                Config.WebHostPhysicalPath = env.WebRootPath ?? env.ContentRootPath;
                Config.DebugMode = env.IsDevelopment();

                //Set VirtualFiles to point to ContentRootPath (Project Folder)
                VirtualFiles = new FileSystemVirtualFiles(env.ContentRootPath);
                AppHostBase.RegisterLicenseFromAppSettings(AppSettings);
                Config.MetadataRedirectPath = "metadata";
            }
        }

        public virtual async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            //Keep in sync with AppHostBase.NetCore.cs
            var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";
            var pathInfo = context.Request.Path.HasValue
                ? context.Request.Path.Value
                : "/";

            var mode = Config.HandlerFactoryPath;
            if (!string.IsNullOrEmpty(mode))
            {
                if (pathInfo.IndexOf(mode, StringComparison.Ordinal) != 1)
                {
                    await next();
                    return;
                }

                pathInfo = pathInfo.Substring(mode.Length + 1);
            }

            RequestContext.Instance.StartRequestContext();

            NetCoreRequest httpReq;
            IResponse httpRes;
            System.Web.IHttpHandler handler;

            try 
            {
                httpReq = new NetCoreRequest(context, operationName, RequestAttributes.None, pathInfo); 
                httpReq.RequestAttributes = httpReq.GetAttributes();
                httpRes = httpReq.Response;
                handler = HttpHandlerFactory.GetHandler(httpReq);
            } 
            catch (Exception ex) //Request Initialization error
            {
                var logFactory = context.Features.Get<ILoggerFactory>();
                if (logFactory != null)
                {
                    var log = logFactory.CreateLogger(GetType());
                    log.LogError(default(EventId), ex, ex.Message);
                }

                context.Response.ContentType = MimeTypes.PlainText;
                await context.Response.WriteAsync($"{ex.GetType().Name}: {ex.Message}");
                if (Config.DebugMode)
                    await context.Response.WriteAsync($"\nStackTrace:\n{ex.StackTrace}");
                return;
            }

            if (handler is IServiceStackHandler serviceStackHandler)
            {
                if (serviceStackHandler is NotFoundHttpHandler)
                {
                    await next();
                    return;
                }

                if (!string.IsNullOrEmpty(serviceStackHandler.RequestName))
                    operationName = serviceStackHandler.RequestName;

                if (serviceStackHandler is RestHandler restHandler)
                {
                    httpReq.OperationName = operationName = restHandler.RestPath.RequestType.GetOperationName();
                }

                try
                {
                    await serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, operationName);
                }
                catch (Exception ex)
                {
                    var logFactory = context.Features.Get<ILoggerFactory>();
                    if (logFactory != null)
                    {
                        var log = logFactory.CreateLogger(GetType());
                        log.LogError(default(EventId), ex, ex.Message);
                    }
                }
                finally
                {
                    httpRes.Close();
                }
                //Matches Exceptions handled in HttpListenerBase.InitTask()

                return;
            }

            await next();
        }

        public override ServiceStackHost Init()
        {
            return this; //Run Init() after Bind()
        }

        protected void RealInit()
        {
            base.Init();
        }

        private string pathBase;
        private string ParsePathBase(string urlBase)
        {
            var pos = urlBase.IndexOf('/', "https://".Length);
            if (pos >= 0)
            {
                var afterHost = urlBase.Substring(pos);
                if (afterHost.Length > 1)
                {
                    pathBase = afterHost;
                    return urlBase.Substring(0, pos + 1);
                }
            }

            return urlBase;
        }

        public override ServiceStackHost Start(string urlBase)
        {
            urlBase = ParsePathBase(urlBase);

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
            LogManager.LogFactory = null;
        }
    }    
}
