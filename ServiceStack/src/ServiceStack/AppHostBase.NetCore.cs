#nullable enable
#if NETCORE


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Host.NetCore;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.NetCore;
using ServiceStack.Platforms;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;
#if NETSTANDARD2_0
using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.Extensions.Hosting;
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
using IHostApplicationLifetime = Microsoft.Extensions.Hosting.IHostApplicationLifetime;
#endif

namespace ServiceStack;

public abstract class AppHostBase : ServiceStackHost, IAppHostNetCore, IConfigureServices, IRequireConfiguration
{
    protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    {
        PlatformNetCore.HostInstance = this;

        //IIS Mapping / sometimes UPPER CASE https://serverfault.com/q/292335
        var iisPathBase = Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH");
        if (!string.IsNullOrEmpty(iisPathBase) && !iisPathBase.Any(char.IsLower))
            iisPathBase = iisPathBase.ToLower();
        PathBase = iisPathBase;
    }

    private string? pathBase;

    public override string? PathBase
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

    IApplicationBuilder? app;

    public IApplicationBuilder App => app ?? throw new ArgumentNullException(nameof(app));
    public IServiceProvider ApplicationServices => App.ApplicationServices;

    public Func<NetCoreRequest, Task>? BeforeNextMiddleware { get; set; }

    /// <summary>
    /// Register dependencies in ServiceStack IOC, to register dependencies in ASP .NET Core IOC implement IHostingStartup
    /// and register services in ConfigureServices(IServiceCollection)
    /// </summary>
    /// <param name="container"></param>
    public override void Configure(Container container) => Configure();

    public virtual void Configure() {}

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
            // Can be registered in services.AddServiceStack()
            var appSettings = app.ApplicationServices.GetService<IAppSettings>()
                ?? new NetCoreAppSettings(configuration);
            if (appHost.AppSettings is AppSettings) // override if default
                appHost.AppSettings = appSettings;
        }
        else
        {
            configuration = (appHost.AppSettings as NetCoreAppSettings)?.Configuration;
            if (appHost is IRequireConfiguration requiresConfig && configuration != null)
                requiresConfig.Configuration = configuration;
        }

        var appLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        appLifetime?.ApplicationStarted.Register(appHost.OnApplicationStarted);
        appLifetime?.ApplicationStopping.Register(appHost.OnApplicationStopping);
    }

    public override void OnApplicationStarted()
    {
        AppTasks.Run(onExit: () => {
            var appLifetime = ApplicationServices.GetService<IHostApplicationLifetime>();
            appLifetime?.StopApplication();
        });
    }

    /// <summary>
    /// The FilePath used in Virtual File Sources
    /// </summary>
    public override string GetWebRootPath()
    {
        if (app == null)
            return base.GetWebRootPath();

        return HostingEnvironment.WebRootPath;
    }

    private IWebHostEnvironment? env;

    public IWebHostEnvironment HostingEnvironment =>
        (env ??= ApplicationServices.GetService<IWebHostEnvironment>()) ?? throw new ArgumentNullException(nameof(env));

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

    public Func<HttpContext, Task<bool>>? NetCoreHandler { get; set; }

    public bool InjectRequestContext { get; set; } = true;
    
    /// <summary>
    /// Whether ServiceStack should ignore handling request
    /// </summary>
    public Func<HttpContext, bool>? IgnoreRequestHandler { get; set; }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Options for app.UseServiceStack(new AppHost(), options => { ... })
    /// </summary>
    public ServiceStackOptions Options { get; set; } = new();
    
    public readonly Dictionary<string, string[]> EndpointVerbs = new()
    {
        [HttpMethods.Get] = [HttpMethods.Get],
        [HttpMethods.Post] = [HttpMethods.Post],
        [HttpMethods.Put] = [HttpMethods.Put],
        [HttpMethods.Delete] = [HttpMethods.Delete],
        [HttpMethods.Patch] = [HttpMethods.Patch],
    };
    
    /// <summary>
    /// Whether to use ASP.NET Core Endpoint Route to invoke ServiceStack APIs
    /// </summary>
    public virtual bool ShouldUseEndpointRoute(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var wildcardEndpoint = endpoint is Microsoft.AspNetCore.Routing.RouteEndpoint routeEndpoint && 
                               routeEndpoint.RoutePattern.RawText?.StartsWith("/{") == true &&
                               routeEndpoint.RoutePattern.RawText?.EndsWith('}') == true;
        var useExistingNonWildcardEndpoint = endpoint != null && !wildcardEndpoint;
        return useExistingNonWildcardEndpoint;
    }
    
    public virtual RouteHandlerBuilder ConfigureOperationEndpoint(RouteHandlerBuilder builder, Operation operation)
    {
        if (operation.ResponseType != null)
        {
            if (operation.ResponseType == typeof(byte[]) || operation.ResponseType == typeof(Stream))
            {
                builder.Produces(200, responseType:operation.ResponseType, contentType:MimeTypes.Binary);
            }
            else
            {
                builder.Produces(200, responseType:operation.ResponseType, contentType:MimeTypes.Json);
            }
        }
        else
        {
            builder.Produces(Config.Return204NoContentForEmptyResponse ? 204 : 200, responseType:null);
        }
        if (operation.RequiresAuthentication)
        {
            var authAttr = operation.Authorize ?? new Microsoft.AspNetCore.Authorization.AuthorizeAttribute();
            authAttr.AuthenticationSchemes ??= Options.AuthenticationSchemes;
            builder.RequireAuthorization(authAttr);
        }
        else
        {
            builder.AllowAnonymous();
        }
            
        if (operation.RequestType.ExcludesFeature(Feature.Metadata) || 
            operation.RequestType.ExcludesFeature(Feature.ApiExplorer))
            builder.ExcludeFromDescription();

        return builder;
    }
    
    public virtual void RegisterEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routeBuilder)
    {
        if (Options.UseEndpointRouting)
        {
            IgnoreRequestHandler = ShouldUseEndpointRoute;
        }

        MapUserDefinedRoutes(routeBuilder);
    }

    public virtual void MapUserDefinedRoutes(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routeBuilder)
    {
        Task HandleRequestAsync(Type requestType, HttpContext httpContext)
        {
            var req = httpContext.ToRequest();
            var restPath = RestHandler.FindMatchingRestPath(req, out var contentType);
            var handler = restPath != null
                ? (HttpAsyncTaskHandler)new RestHandler {
                    RestPath = restPath, 
                    RequestName = restPath.RequestType.GetOperationName(), 
                    ResponseContentType = contentType
                }
                : new NotFoundHttpHandler();
            return handler.ProcessRequestAsync(req, req.Response, requestType.Name);
        }
        
        foreach (var entry in Metadata.OperationsMap)
        {
            var requestType = entry.Key;
            var operation = entry.Value;

            if (!EndpointVerbs.TryGetValue(operation.Method, out var verb))
                continue;

            foreach (var route in operation.Routes.Safe())
            {
                var routeVerbs = route.Verbs == null || (route.Verbs.Length == 1 && route.Verbs[0] == ActionContext.AnyAction) 
                    ? [operation.Method]
                    : route.Verbs;

                foreach (var routeVerb in routeVerbs)
                {
                    if (!EndpointVerbs.TryGetValue(routeVerb, out verb))
                        continue;

                    var pathBuilder = routeBuilder.MapMethods(route.Path, verb, (HttpResponse response, HttpContext httpContext) =>
                        HandleRequestAsync(requestType, httpContext));
                    
                    ConfigureOperationEndpoint(pathBuilder, operation);

                    foreach (var handler in Options.RouteHandlerBuilders)
                    {
                        handler(pathBuilder, operation, routeVerb, route.Path);
                    }
                }
                
                // Add /custom/path.{format} routes for GET requests
                if (routeVerbs.Contains(HttpMethods.Get) && !route.Path.Contains('.'))
                {
                    var pathBuilder = routeBuilder.MapMethods(route.Path + ".{format}", EndpointVerbs[HttpMethods.Get], 
                            (string format, HttpResponse response, HttpContext httpContext) =>
                        HandleRequestAsync(requestType, httpContext));
                    
                    ConfigureOperationEndpoint(pathBuilder, operation)
                        .WithMetadata<string>(route.Path + ".format");
                }
            }
        }
    }
#endif

    public virtual async Task ProcessRequest(HttpContext context, Func<Task> next)
    {
        if (IgnoreRequestHandler != null && IgnoreRequestHandler(context))
        {
            await next().ConfigAwait();
            return;
        }
        
        if (NetCoreHandler != null)
        {
            var handled = await NetCoreHandler(context).ConfigAwait();
            if (handled)
                return;
        }

        //Keep in sync with Kestrel/AppSelfHostBase.cs
        var operationName = context.Request.GetOperationName() ?? "Home"; //already decoded
        var pathInfo = context.Request.Path.HasValue
            ? context.Request.Path.Value!
            : "/";

        var mode = Config.HandlerFactoryPath;
        if (!string.IsNullOrEmpty(mode))
        {
            //IIS Reports "ASPNETCORE_APPL_PATH" in UPPER CASE
            var includedInPathInfo = pathInfo.IndexOf(mode, StringComparison.OrdinalIgnoreCase) == 1;
            var includedInPathBase = context.Request.PathBase.HasValue &&
                                     context.Request.PathBase.Value!.IndexOf(mode,
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

#if NET8_0_OR_GREATER
            // Only use fallback handlers when ServiceStack Routing is disabled
            if (Options.DisableServiceStackRouting)
            {
                if (GetCatchAllHandler(httpReq) is HttpAsyncTaskHandler catchAllHandler)
                {
                    await context.ProcessRequestAsync(catchAllHandler).ConfigAwait();
                    return;
                }

                if (GetFallbackHandler(httpReq) is HttpAsyncTaskHandler fallbackHandler)
                {
                    await context.ProcessRequestAsync(fallbackHandler).ConfigAwait();
                    return;
                }
            }
#endif

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

#if NET8_0_OR_GREATER            
            if (Options.DisableServiceStackRouting)
            {
                Log.WarnFormat("ServiceStack Routing is disabled, request to {0} by {1} is ignored", httpReq.PathInfo, handler.GetType().Name);
                await next().ConfigAwait();
                return;
            }
#endif

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
        if (relativePath.StartsWith("~"))
            return Path.GetFullPath(HostingEnvironment.ContentRootPath.CombineWith(relativePath.Substring(1)));

        return relativePath.MapHostAbsolutePath();
    }

    public override IRequest? TryGetCurrentRequest()
    {
        return GetOrCreateRequest(ApplicationServices.GetRequiredService<IHttpContextAccessor>());
    }

    /// <summary>
    /// Creates an IRequest from IHttpContextAccessor if it's been registered as a singleton
    /// </summary>
    public static IRequest? GetOrCreateRequest(IHttpContextAccessor httpContextAccessor) => httpContextAccessor.GetOrCreateRequest();

    public static IRequest? GetOrCreateRequest(HttpContext httpContext) => httpContext.GetOrCreateRequest();

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

#if NET8_0_OR_GREATER

public delegate void RouteHandlerBuilderDelegate(RouteHandlerBuilder builder, Operation operation, string verb, string route);

#endif

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
        Action<ServiceStackHost>? beforeConfigure = null,
        Action<ServiceStackHost>? afterConfigure = null,
        Action<ServiceStackHost>? afterPluginsLoaded = null,
        Action<ServiceStackHost>? afterAppHostInit = null)
    {
        HostContext.ConfigureAppHost(
            beforeConfigure:beforeConfigure,
            afterConfigure:afterConfigure,
            afterPluginsLoaded:afterPluginsLoaded,
            afterAppHostInit:afterAppHostInit);
        return builder;
    }

#if NET8_0_OR_GREATER
    public static void AddServiceStack(this IServiceCollection services, Assembly serviceAssembly, Action<ServiceStackServicesOptions>? configure = null) =>
        services.AddServiceStack([serviceAssembly], configure);
    
    public static void AddServiceStack(this IServiceCollection services, IEnumerable<Assembly>? serviceAssemblies, Action<ServiceStackServicesOptions>? configure = null)
    {
        var options = ServiceStackHost.InitOptions;
        options.UseServices(services);
        if (serviceAssemblies != null)
            options.ServiceAssemblies.AddRange(serviceAssemblies);
        configure?.Invoke(options);

        options.ConfigurePlugins(services);

        var allServiceTypes = options.GetAllServiceTypes();
        foreach (var type in allServiceTypes)
        {
            services.AddTransient(type);
        }
        
        ServiceStackHost.GlobalAfterConfigureServices.ForEach(fn => fn(services));
        ServiceStackHost.GlobalAfterConfigureServices.Clear();

        if (options.ShouldAutoRegister<IAppSettings>() && !services.Exists<IAppSettings>())
        {
            services.AddSingleton<IAppSettings, NetCoreAppSettings>();
        }
        if (options.ShouldAutoRegister<ICacheClient>() && !services.Exists<ICacheClient>())
        {
            if (services.Exists<IRedisClientsManager>())
                services.AddSingleton<ICacheClient>(c => c.GetRequiredService<IRedisClientsManager>().GetCacheClient());
            else
                services.AddSingleton<ICacheClient>(ServiceStackHost.DefaultCache);
        }
        if (options.ShouldAutoRegister<ICacheClientAsync>() && !services.Exists<ICacheClientAsync>())
        {
            services.AddSingleton<ICacheClientAsync>(c => c.GetRequiredService<ICacheClient>().AsAsync());
        }
        if (options.ShouldAutoRegister<MemoryCacheClient>() && !services.Exists<MemoryCacheClient>())
        {
            services.AddSingleton(ServiceStackHost.DefaultCache);
        }
        if (options.ShouldAutoRegister<IMessageFactory>() && !services.Exists<IMessageFactory>() && !services.Exists<IMessageService>())
        {
            services.AddSingleton<IMessageFactory>(c => c.GetRequiredService<IMessageService>().MessageFactory);
        }
    }
#endif

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

    public static IApplicationBuilder UseServiceStack(this IApplicationBuilder app, AppHostBase appHost
#if NET8_0_OR_GREATER
        , Action<ServiceStackOptions>? configure = null
#endif
    )
    {
        // Manually simulating Modular Startup when using .NET 6+ top-level statements app builder
        if (TopLevelAppModularStartup.Instance != null)
        {
            TopLevelAppModularStartup.Instance.Configure(app);
        }
        
#if NET8_0_OR_GREATER
        configure?.Invoke(appHost.Options);

        var appOptions = app.ApplicationServices.GetServices<IConfigureOptions<ServiceStackOptions>>();
        foreach (var appOption in appOptions)
        {
            appOption.Configure(appHost.Options);
        }
#endif
        appHost.Bind(app);
        appHost.Init();

#if NET8_0_OR_GREATER
        if (ServiceStackHost.InitOptions.RegisterServicesInServiceCollection)
        {
            appHost.Container.CheckAdapterFirst = true;
        }
        
        if (appHost.Options.MapEndpointRouting)
        {
            if (app is Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routeBuilder)
            {
                appHost.RegisterEndpoints(routeBuilder);
            }
        }
#endif
        
        return app;
    }
    
#if NET8_0_OR_GREATER
    public static void MapEndpoints(this AppHostBase appHost, Action<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder> configure)
    {
        if (!appHost.Options.MapEndpointRouting)
            return;
        
        configure((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)appHost.App);
    }
    
    public static Task ProcessRequestAsync(this HttpContext httpContext, Func<IRequest,HttpAsyncTaskHandler?>? handlerFactory, string? apiName=null, Action<IRequest>? configure = null)
    {
        if (handlerFactory == null)
            return Task.CompletedTask;
        var req = httpContext.ToRequest();
        var handler = handlerFactory(req);
        if (handler == null)
            return Task.CompletedTask;
        var res = (NetCoreResponse)req.Response;
        //res.KeepOpen = true; // Let ASP.NET Core close request
        configure?.Invoke(req);
        return handler.ProcessRequestAsync(req, req.Response, apiName ?? httpContext.Request.Path);
    }

    public static Task ProcessRequestAsync(this HttpContext httpContext, HttpAsyncTaskHandler? handler, string? apiName=null, Action<IRequest>? configure = null)
    {
        if (handler == null)
            return Task.CompletedTask;
        var req = httpContext.ToRequest();
        var res = (NetCoreResponse)req.Response;
        //res.KeepOpen = true; // Let ASP.NET Core close request
        configure?.Invoke(req);
        return handler.ProcessRequestAsync(req, req.Response, apiName ?? httpContext.Request.Path);
    }

    public static IEndpointConventionBuilder WithMetadata<TResponse>(this IEndpointConventionBuilder builder,
        string? name=null,
        string? tag=null,
        string? description=null,
        string? contentType=null,
        string[]? additionalContentTypes=null,
        bool exclude=true) =>
        builder.WithMetadata(name:name,
            tag:tag,
            description:description,
            responseType:typeof(TResponse),
            contentType:contentType,
            additionalContentTypes:additionalContentTypes,
            exclude:true);
    
    public static IEndpointConventionBuilder WithMetadata(this IEndpointConventionBuilder builder,
        string? name=null,
        string? tag=null,
        string? description=null,
        Type? responseType=null,
        string? contentType=null,
        string[]? additionalContentTypes=null,
        bool exclude=true)
    {
        if (name != null)
            builder.WithName(name);
        if (tag != null)
            builder.WithTags(tag);
        if (description != null)
            builder.WithDescription(description);
        if (responseType != null)
        {
            var routeBuilder = (RouteHandlerBuilder)builder;
            routeBuilder.Produces(200, responseType, contentType: contentType, additionalContentTypes: additionalContentTypes ?? Array.Empty<string>());
        }
        if (exclude)
            builder.ExcludeFromDescription();
        return builder;
    }
#endif

    public static IApplicationBuilder Use(this IApplicationBuilder app, IHttpAsyncHandler httpHandler)
    {
        return app.Use(httpHandler.Middleware);
    }

    public static IHttpRequest ToRequest(this HttpContext httpContext, string? operationName = null)
    {
        var req = new NetCoreRequest(httpContext, operationName);
        req.RequestAttributes = req.GetAttributes() | RequestAttributes.Http;
        return req;
    }

    public static T? TryResolve<T>(this IServiceProvider provider) => provider.GetService<T>();
    public static T Resolve<T>(this IServiceProvider provider) where T : notnull => provider.GetRequiredService<T>();

    public static IHttpRequest ToRequest(this HttpRequest request, string? operationName = null) =>
        request.HttpContext.ToRequest();

    public static T? TryResolveScoped<T>(this IRequest req) => ((IServiceProvider)req).GetService<T>();
    public static object? TryResolveScoped(this IRequest req, Type type) => ((IServiceProvider)req).GetService(type);
    public static T ResolveScoped<T>(this IRequest req) where T : notnull => ((IServiceProvider)req).GetRequiredService<T>();

    public static object ResolveScoped(this IRequest req, Type type) =>
        ((IServiceProvider)req).GetRequiredService(type);

    public static IServiceScope CreateScope(this IRequest req) => ((IServiceProvider)req).CreateScope();

    public static IEnumerable<object?> GetServices(this IRequest req, Type type) =>
        ((IServiceProvider)req).GetServices(type);

    public static IEnumerable<T> GetServices<T>(this IRequest req) => ((IServiceProvider)req).GetServices<T>();

    /// <summary>
    /// Creates an IRequest from IHttpContextAccessor if it's been registered as a singleton
    /// </summary>
    public static IRequest? GetOrCreateRequest(this IHttpContextAccessor httpContextAccessor)
    {
        return GetOrCreateRequest(httpContextAccessor?.HttpContext);
    }

    public static IRequest? GetOrCreateRequest(this HttpContext? httpContext)
    {
        if (httpContext != null)
        {
            if (httpContext.Items.TryGetValue(Keywords.IRequest, out var oRequest))
                return oRequest as IRequest;

            var req = httpContext.ToRequest();
            httpContext.Items[Keywords.IRequest] = req;

            return req;
        }
        return null;
    }
    
#if NET6_0_OR_GREATER
    public static T ConfigureAndResolve<T>(this IHostingStartup config, string? hostDir = null, bool setHostDir = true)
    {
        var holdCurrentDir = Environment.CurrentDirectory;
        var host = new HostBuilder()
            .ConfigureHostConfiguration(hostConfig => {
                if (hostDir == null)
                {
                    // Allow local appsettings.json to override HostDir 
                    var localConfigPath = Path.GetFullPath("appsettings.json");
                    if (File.Exists(localConfigPath))
                    {
                        var appSettings = JSON.parse(File.ReadAllText(localConfigPath));
                        if (appSettings is Dictionary<string, object> map &&
                            map.TryGetValue("HostDir", out var oHostDir) && oHostDir is string s)
                        {
                            hostDir = Path.GetFullPath(s);
                            // File based connection to relative App_Data/db.sqlite requires cd
                            if (setHostDir)
                                Directory.SetCurrentDirectory(hostDir);
                        }
                    }
                }
                
                hostDir ??= Path.GetDirectoryName(config.GetType().Assembly.Location);
                if (!Directory.Exists(hostDir)) return;
                hostConfig.SetBasePath(hostDir);
                var devAppSettingsPath = Path.Combine(hostDir, "appsettings.Development.json");
                hostConfig.AddJsonFile(devAppSettingsPath, optional:true, reloadOnChange:false);
                var appSettingsPath = Path.Combine(hostDir, "appsettings.json");
                hostConfig.AddJsonFile(appSettingsPath, optional:true, reloadOnChange:false);
            })
            .ConfigureWebHost(builder => {
                config.Configure(builder);
            }).Build();

        var service = host.Services.GetRequiredService<T>();
        return service;
    }
#endif
    
}

#endif
