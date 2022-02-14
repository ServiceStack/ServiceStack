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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Host.NetCore;
using ServiceStack.IO;
using ServiceStack.Web;

#if NETSTANDARD2_0
using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.Extensions.Hosting;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
using IHostApplicationLifetime = Microsoft.Extensions.Hosting.IHostApplicationLifetime;
#endif

namespace ServiceStack
{
    public abstract class AppSelfHostBase : ServiceStackHost, IAppHostNetCore, IConfigureServices, IRequireConfiguration
    {
        public IConfiguration Configuration { get; set; }
        
        protected AppSelfHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) 
        {
            Platforms.PlatformNetCore.HostInstance = this;
        }
        
        protected AppSelfHostBase(string serviceName, params Type[] serviceTypes)
            : base(serviceName, TypeConstants<Assembly>.EmptyArray) 
        {
            Platforms.PlatformNetCore.HostInstance = this;
            ServiceController = CreateServiceController(serviceTypes);
        }

        private string pathBase;
        public override string PathBase
        {
            get => pathBase;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (value[0] != '/')
                        throw new Exception("PathBase must start with '/'");
                    
                    pathBase = value.TrimEnd('/');
                }
                else
                {
                    pathBase = null;
                }
            }
        }

        IApplicationBuilder app;
        public IApplicationBuilder App => app;
        public IServiceProvider ApplicationServices => app?.ApplicationServices;

        private IWebHostEnvironment env;
        public IWebHostEnvironment HostingEnvironment => env ??= app?.ApplicationServices.GetService<IWebHostEnvironment>();

        public virtual void Bind(IApplicationBuilder app)
        {
            this.app = app;

            if (!string.IsNullOrEmpty(PathBase))
            {
                this.app.UsePathBase(PathBase);
            }

            AppHostBase.BindHost(this, app);
            app.Use(ProcessRequest);
        }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();
            if (app != null)
            {
                Config.DebugMode = HostingEnvironment.IsDevelopment();
                Config.HandlerFactoryPath = PathBase?.TrimStart('/');

                //Initialize VFS
                Config.WebHostPhysicalPath = HostingEnvironment.ContentRootPath;

                if (VirtualFiles == null)
                {
                    //Set VirtualFiles to point to ContentRootPath (Project Folder)
                    VirtualFiles = new FileSystemVirtualFiles(HostingEnvironment.ContentRootPath);
                }
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
                var includedInPathInfo = pathInfo.IndexOf(mode, StringComparison.Ordinal) == 1;
                var includedInPathBase = context.Request.PathBase.HasValue &&
                                         context.Request.PathBase.Value.IndexOf(mode, StringComparison.Ordinal) == 1;
                if (!includedInPathInfo && !includedInPathBase)
                {
                    await next();
                    return;
                }

                if (includedInPathInfo)
                    pathInfo = pathInfo.Substring(mode.Length + 1);
            }

            RequestContext.Instance.StartRequestContext();

            NetCoreRequest httpReq;
            IResponse httpRes;
            IHttpHandler handler;

            try 
            {
                httpReq = new NetCoreRequest(context, operationName, RequestAttributes.None, pathInfo); 
                httpReq.RequestAttributes = httpReq.GetAttributes() | RequestAttributes.Http;
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
                    await httpRes.CloseAsync();
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

        private string ParsePathBase(string urlBase)
        {
            var pos = urlBase.IndexOf('/', "https://".Length);
            if (pos >= 0)
            {
                var afterHost = urlBase.Substring(pos);
                if (afterHost.Length > 1)
                {
                    PathBase = afterHost;
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
            return host.UseKestrel(ConfigureKestrel)
                .UseContentRoot(System.IO.Directory.GetCurrentDirectory())
                .UseWebRoot(System.IO.Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(urlBases);
        }
        
        public virtual void ConfigureKestrel(KestrelServerOptions options) {}

        /// <summary>
        /// Override to Configure .NET Core dependencies
        /// </summary>
        public virtual void Configure(IServiceCollection services) {}

        /// <summary>
        /// Override to Configure .NET Core App
        /// </summary>
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Configure(app);
        }
        
        public virtual void Configure(IApplicationBuilder app) {}

        public static AppSelfHostBase HostInstance => (AppSelfHostBase)Platforms.PlatformNetCore.HostInstance;

        protected class Startup
        {
            public IConfiguration Configuration { get; }
            public Startup(IConfiguration configuration) => Configuration = configuration;

            public void ConfigureServices(IServiceCollection services)
            {
                HostInstance.Configuration = Configuration;
                HostInstance.Configure(services);
            }

            public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
