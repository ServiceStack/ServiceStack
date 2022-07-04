#if NETCORE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Web;
using ServiceStack.Logging;
using ServiceStack.NetCore;
using ServiceStack.Host;
using ServiceStack.Host.NetCore;
using ServiceStack.Host.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Text;
using Microsoft.AspNetCore.Hosting;

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
    public abstract class AppHostBase : ServiceStackHost, IAppHostNetCore, IConfigureServices, IRequireConfiguration
    {
        protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            Platforms.PlatformNetCore.HostInstance = this;

            //IIS Mapping / sometimes UPPER CASE https://serverfault.com/q/292335
            var iisPathBase = Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH");
            if (!string.IsNullOrEmpty(iisPathBase) && !iisPathBase.Any(char.IsLower))
                iisPathBase = iisPathBase.ToLower();
            PathBase = iisPathBase;
        }

        private string pathBase;

        public override string PathBase
        {
            get => pathBase ?? Config?.HandlerFactoryPath;
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

        public Func<NetCoreRequest, Task> BeforeNextMiddleware { get; set; }

        public virtual void Bind(IApplicationBuilder app)
        {
            this.app = app;

            if (!string.IsNullOrEmpty(PathBase))
            {
                this.app.UsePathBase(PathBase);
            }

            BindHost(this, app);
            app.Use(ProcessRequest);
        }

        public override void ConfigureLogging()
        {
            var logFactory = app?.ApplicationServices.GetService<ILoggerFactory>();
            if (logFactory != null)
            {
                NetCoreLogFactory.FallbackLoggerFactory = logFactory;
                if (LogManager.LogFactory.IsNullOrNullLogFactory())
                    LogManager.LogFactory = new NetCoreLogFactory(logFactory);
            }
            base.ConfigureLogging();
        }

        public static void BindHost(ServiceStackHost appHost, IApplicationBuilder app)
        {
            appHost.ConfigureLogging();

            appHost.Container.Adapter = new NetCoreContainerAdapter(app.ApplicationServices);

            // Auto populate AppSettings with NetCoreAppSettings(IConfiguration)
            var configuration = app.ApplicationServices.GetService<IConfiguration>();
            if (configuration != null)
            {
                if (appHost.AppSettings is AppSettings) // override if default
                    appHost.AppSettings = new NetCoreAppSettings(configuration);
            }
            else
            {
                configuration = (appHost.AppSettings as NetCoreAppSettings)?.Configuration;
                if (appHost is IRequireConfiguration requiresConfig)
                    requiresConfig.Configuration = configuration;
            }

            var appLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            appLifetime?.ApplicationStopping.Register(appHost.OnApplicationStopping);
        }

        /// <summary>
        /// The FilePath used in Virtual File Sources
        /// </summary>
        public override string GetWebRootPath()
        {
            if (app == null)
                return base.GetWebRootPath();

            return HostingEnvironment.WebRootPath ?? HostingEnvironment.ContentRootPath;
        }

        private IWebHostEnvironment env;

        public IWebHostEnvironment HostingEnvironment =>
            env ??= app?.ApplicationServices.GetService<IWebHostEnvironment>();

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

                RegisterLicenseFromAppSettings(AppSettings);
                InjectRequestContext = app?.ApplicationServices.GetService<IHttpContextAccessor>() != null;
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

        public Func<HttpContext, Task<bool>> NetCoreHandler { get; set; }

        public bool InjectRequestContext { get; set; }

        public virtual async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            if (NetCoreHandler != null)
            {
                var handled = await NetCoreHandler(context).ConfigAwait();
                if (handled)
                    return;
            }

            //Keep in sync with Kestrel/AppSelfHostBase.cs
            var operationName = context.Request.GetOperationName() ?? "Home"; //already decoded
            var pathInfo = context.Request.Path.HasValue
                ? context.Request.Path.Value
                : "/";

            var mode = Config.HandlerFactoryPath;
            if (!string.IsNullOrEmpty(mode))
            {
                //IIS Reports "ASPNETCORE_APPL_PATH" in UPPER CASE
                var includedInPathInfo = pathInfo.IndexOf(mode, StringComparison.OrdinalIgnoreCase) == 1;
                var includedInPathBase = context.Request.PathBase.HasValue &&
                                         context.Request.PathBase.Value.IndexOf(mode,
                                             StringComparison.OrdinalIgnoreCase) == 1;
                if (!includedInPathInfo && !includedInPathBase)
                {
                    await next().ConfigAwait();
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

                if (InjectRequestContext)
                    context.Items[Keywords.IRequest] = httpReq;

                if (BeforeNextMiddleware != null)
                {
                    var holdNext = next;
                    next = async () => {
                        await BeforeNextMiddleware(httpReq).ConfigAwait();
                        await holdNext().ConfigAwait();
                    };
                }
            }
            catch (Exception ex) //Request Initialization error
            {
                var logFactory = context.Features.Get<ILoggerFactory>();
                if (logFactory != null)
                {
                    var log = logFactory.CreateLogger(GetType());
                    log.LogError(default, ex, ex.Message);
                }

                context.Response.ContentType = MimeTypes.PlainText;
                await context.Response.WriteAsync($"{ex.GetType().Name}: {ex.Message}").ConfigAwait();
                if (Config.DebugMode)
                    await context.Response.WriteAsync($"\nStackTrace:\n{ex.StackTrace}").ConfigAwait();
                return;
            }

            if (handler is IServiceStackHandler serviceStackHandler)
            {
                if (serviceStackHandler is NotFoundHttpHandler)
                {
                    await next().ConfigAwait();
                    return;
                }
    
                try
                {
                    await serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, httpReq.OperationName).ConfigAwait();
                }
                catch (Exception ex)
                {
                    var logFactory = context.Features.Get<ILoggerFactory>();
                    if (logFactory != null)
                    {
                        var log = logFactory.CreateLogger(GetType());
                        log.LogError(default, ex, ex.Message);
                    }
                }
                finally
                {
                    await httpRes.CloseAsync().ConfigAwait();
                }
                //Matches Exceptions handled in HttpListenerBase.InitTask()

                return;
            }

            await next().ConfigAwait();
        }

        public override string MapProjectPath(string relativePath)
        {
            if (HostingEnvironment?.ContentRootPath != null && relativePath?.StartsWith("~") == true)
                return Path.GetFullPath(HostingEnvironment.ContentRootPath.CombineWith(relativePath.Substring(1)));

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
                if (httpContext.Items.TryGetValue(Keywords.IRequest, out var oRequest))
                    return (IRequest)oRequest;

                var req = httpContext.ToRequest();
                httpContext.Items[Keywords.IRequest] = req;

                return req;
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) LogManager.LogFactory = null;
        }

        /// <summary>
        /// Requires using ModularStartup
        /// </summary>
        /// <param name="services"></param>
        public virtual void Configure(IServiceCollection services) { }

        public IConfiguration Configuration { get; set; }
    }

    public interface IAppHostNetCore : IAppHost, IRequireConfiguration
    {
        IApplicationBuilder App { get; }
#if NETSTANDARD2_0
        IWebHostEnvironment HostingEnvironment { get; }
#else
        IWebHostEnvironment HostingEnvironment { get; }
#endif
    }

    public static class NetCoreAppHostExtensions
    {
        /// <summary>
        /// Register static callbacks fired just after AppHost.Configure()
        /// <param name="builder"></param>
        /// <param name="beforeConfigure">Register static callbacks fired just before AppHost.Configure()</param> 
        /// <param name="afterConfigure">Register static callbacks fired just after AppHost.Configure()</param> 
        /// <param name="afterPluginsLoaded">Register static callbacks fired just after plugins are loaded</param> 
        /// <param name="afterAppHostInit">Register static callbacks fired after the AppHost is initialized</param> 
        /// </summary>
        public static IWebHostBuilder ConfigureAppHost(this IWebHostBuilder builder,
            Action<ServiceStackHost> beforeConfigure = null,
            Action<ServiceStackHost> afterConfigure = null,
            Action<ServiceStackHost> afterPluginsLoaded = null,
            Action<ServiceStackHost> afterAppHostInit = null)
        {
            HostContext.ConfigureAppHost(
                beforeConfigure:beforeConfigure,
                afterConfigure:afterConfigure,
                afterPluginsLoaded:afterPluginsLoaded,
                afterAppHostInit:afterAppHostInit);
            return builder;
        }

        public static IConfiguration GetConfiguration(this IAppHost appHost) =>
            ((IAppHostNetCore)appHost).Configuration;

        public static IApplicationBuilder GetApp(this IAppHost appHost) => ((IAppHostNetCore)appHost).App;

        public static IServiceProvider GetApplicationServices(this IAppHost appHost) =>
            ((IAppHostNetCore)appHost).App.ApplicationServices;

        public static IWebHostEnvironment GetHostingEnvironment(this IAppHost appHost) =>
            ((IAppHostNetCore)appHost).HostingEnvironment;

        public static bool IsDevelopmentEnvironment(this IAppHost appHost) =>
            appHost.GetHostingEnvironment().EnvironmentName == "Development";

        public static bool IsStagingEnvironment(this IAppHost appHost) =>
            appHost.GetHostingEnvironment().EnvironmentName == "Staging";

        public static bool IsProductionEnvironment(this IAppHost appHost) =>
            appHost.GetHostingEnvironment().EnvironmentName == "Production";

        public static IApplicationBuilder UseServiceStack(this IApplicationBuilder app, AppHostBase appHost)
        {
            // Manually simulating Modular Startup when using .NET 6+ top-level statements app builder
            if (TopLevelAppModularStartup.Instance != null)
            {
                TopLevelAppModularStartup.Instance.Configure(app);
            }

            appHost.Bind(app);
            appHost.Init();
            return app;
        }

        public static IApplicationBuilder Use(this IApplicationBuilder app, IHttpAsyncHandler httpHandler)
        {
            return app.Use(httpHandler.Middleware);
        }

        public static IHttpRequest ToRequest(this HttpContext httpContext, string operationName = null)
        {
            var req = new NetCoreRequest(httpContext, operationName, RequestAttributes.None);
            req.RequestAttributes = req.GetAttributes() | RequestAttributes.Http;
            return req;
        }

        public static T TryResolve<T>(this IServiceProvider provider) => provider.GetService<T>();
        public static T Resolve<T>(this IServiceProvider provider) => provider.GetRequiredService<T>();

        public static IHttpRequest ToRequest(this HttpRequest request, string operationName = null) =>
            request.HttpContext.ToRequest();

        public static T TryResolveScoped<T>(this IRequest req) => (T)((IServiceProvider)req).GetService(typeof(T));
        public static object TryResolveScoped(this IRequest req, Type type) => ((IServiceProvider)req).GetService(type);
        public static T ResolveScoped<T>(this IRequest req) => (T)((IServiceProvider)req).GetRequiredService(typeof(T));

        public static object ResolveScoped(this IRequest req, Type type) =>
            ((IServiceProvider)req).GetRequiredService(type);

        public static IServiceScope CreateScope(this IRequest req) => ((IServiceProvider)req).CreateScope();

        public static IEnumerable<object> GetServices(this IRequest req, Type type) =>
            ((IServiceProvider)req).GetServices(type);

        public static IEnumerable<T> GetServices<T>(this IRequest req) => ((IServiceProvider)req).GetServices<T>();
    }
}

#endif