#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Funq;
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
using ServiceStack.Data;
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

namespace ServiceStack;

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
    
    /// <summary>
    /// Whether ServiceStack should ignore handling request
    /// </summary>
    public Func<HttpContext, bool>? IgnoreRequestHandler { get; set; }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Options for app.UseServiceStack(new AppHost(), options => { ... })
    /// </summary>
    public ServiceStackOptions Options { get; set; } = new();
    
    public Dictionary<string, string[]> EndpointVerbs { get; } = new()
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
    
    public virtual RouteHandlerBuilder ConfigureOperationEndpoint(RouteHandlerBuilder builder, Operation operation, EndpointOptions options=default, Type? responseType=null)
    {
        responseType ??= operation.ResponseType; 
        if (responseType != null)
        {
            if (responseType == typeof(byte[]) || responseType == typeof(Stream))
            {
                builder.Produces(200, responseType:responseType, contentType:MimeTypes.Binary);
            }
            else
            {
                builder.Produces(200, responseType:responseType, contentType:MimeTypes.Json);
            }
        }
        else
        {
            builder.Produces(Config.Return204NoContentForEmptyResponse ? 204 : 200, responseType:null);
        }
        if (options.RequireAuth && operation.RequiresAuthentication)
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
            Instance.InitRequest(handler, req);
            return handler.ProcessRequestAsync(req, req.Response, requestType.Name);
        }
        
        var options = CreateEndpointOptions();
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

                string? routeRule = null;
                try
                {
                    foreach (var routeVerb in routeVerbs)
                    {
                        if (!EndpointVerbs.TryGetValue(routeVerb, out verb))
                            continue;

                        routeRule = $"[{verb}] {route.Path}";
                        var pathBuilder = routeBuilder.MapMethods(route.Path, verb, (HttpResponse response, HttpContext httpContext) =>
                            HandleRequestAsync(requestType, httpContext));
                        
                        ConfigureOperationEndpoint(pathBuilder, operation, options);

                        foreach (var handler in Options.RouteHandlerBuilders)
                        {
                            handler(pathBuilder, operation, routeVerb, route.Path);
                        }
                    }
                    
                    // Add /custom/path.{format} routes for GET requests
                    if (routeVerbs.Contains(HttpMethods.Get) && !route.Path.Contains('.') && !route.Path.Contains('*'))
                    {
                        routeRule = $"[GET] {route.Path}.format";
                        var pathBuilder = routeBuilder.MapMethods(route.Path + ".{format}", EndpointVerbs[HttpMethods.Get], 
                            (string format, HttpResponse response, HttpContext httpContext) =>
                                HandleRequestAsync(requestType, httpContext));
                        
                        ConfigureOperationEndpoint(pathBuilder, operation, options)
                            .WithMetadata<string>(route.Path + ".format");
                    }
                }
                catch (Exception e)
                {
                    LogManager.GetLogger(GetType()).Error($"Error mapping route '{routeRule}' for {requestType.Name}: {e.Message}", e);
                    throw;
                }
            }
        }
    }

    public EndpointOptions CreateEndpointOptions()
    {
        var requireAuth = ApplicationServices.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>() != null
            || HasPlugin<AuthFeature>();
        var options = new EndpointOptions(RequireAuth: requireAuth);
        return options;
    }
#endif

    private string? pathBase;
    public override string? PathBase
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

    IApplicationBuilder? app;
    public IApplicationBuilder App => app ?? throw new ArgumentNullException(nameof(app));
    public IServiceProvider ApplicationServices => App.ApplicationServices;

    private IWebHostEnvironment? env;
    public IWebHostEnvironment HostingEnvironment => env ??= app!.ApplicationServices.GetService<IWebHostEnvironment>()
        ?? throw new ArgumentNullException(nameof(env));

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

            try
            {
                await serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, httpReq.OperationName);
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

        return Start([urlBase]);
    }

    public IWebHost? WebHost { get; private set; }

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
    /// Register dependencies in ServiceStack IOC, to register dependencies in ASP .NET Core IOC implement IHostingStartup
    /// and register services in ConfigureServices(IServiceCollection)
    /// </summary>
    /// <param name="container"></param>
    public override void Configure(Funq.Container container) => Configure();

    public virtual void Configure() {}

    /// <summary>
    /// Override to Configure .NET Core App
    /// </summary>
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Configure(app);
    }
    
    public virtual void Configure(IApplicationBuilder app) {}

    public static AppSelfHostBase HostInstance => (AppSelfHostBase)Platforms.PlatformNetCore.HostInstance;

    protected class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

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

public static class AppSelfHostUtils
{
}
