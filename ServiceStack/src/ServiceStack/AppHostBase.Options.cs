using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Script;

#nullable enable

namespace ServiceStack;

public class ServiceStackServicesOptions
{
    public IServiceCollection? Services { get; internal set; }

    internal void UseServices(IServiceCollection services)
    {
        Services = services;
    }
    public bool RegisterServicesInServiceCollection => Services != null;

    internal void ConfigurePlugins(IServiceCollection services)
    {
        var configurePlugins = Plugins.Where(x => !PluginsConfigured.Contains(x)).OfType<IConfigureServices>();
        foreach (var plugin in configurePlugins)
        {
            plugin.Configure(services);
        }

        var postConfigurePlugins = Plugins.Where(x => !PluginsConfigured.Contains(x))
            .OfType<IPostConfigureServices>().OrderBy(x => x.Priority);
        foreach (var plugin in postConfigurePlugins)
        {
            plugin.AfterConfigure(services);
        }
        Plugins.ForEach(x => PluginsConfigured.Add(x));
    }
    
    /// <summary>
    /// The fallback ScriptContext to use if no SharpPagesFeature plugin was registered
    /// </summary>
    public ScriptContext ScriptContext { get; set; } = new ScriptContext {
        ScriptLanguages = { ScriptLisp.Language },
    }.InitForSharpPages();

    /// <summary>
    /// Register Assemblies to scan for ServiceStack Services to load before AppHost Configure
    /// </summary>
    public List<Assembly> ServiceAssemblies { get; } = []; // Same collection as AppHost.ServiceAssemblies

    /// <summary>
    /// Register Service Types to load before AppHost Configure
    /// </summary>
    public List<Type> ServiceTypes { get; } = [];

    /// <summary>
    /// Register ServiceStack Services and user-defined to load before AppHost Configure
    /// </summary>
    public Dictionary<Type, string[]> ServiceRoutes { get; } = new();

    /// <summary>
    /// Auto Register built-in dependencies when not registered
    /// </summary>
    public List<Type> AutoRegister { get; } = [
        typeof(IAppSettings),
        typeof(ICacheClient),
        typeof(ICacheClientAsync),
        typeof(MemoryCacheClient),
        typeof(IMessageFactory),
    ];

    internal bool ShouldAutoRegister<T>() => AutoRegister.Contains(typeof(T));
    
    internal HashSet<Type> ServicesRegistered = [];

    public List<IPlugin> Plugins { get; } = DefaultPlugins();
    internal HashSet<IPlugin> PluginsConfigured = [];

    internal HashSet<Type> GetAllServiceTypes()
    {
        var to = ServiceAssemblies.SelectMany(x => x.GetTypes().Where(ServiceController.IsServiceType)).ToSet();
        to.AddDistinctRange(ServiceTypes);
        to.AddDistinctRange(ServiceRoutes.Keys);
        return to;
    }
    
    /// <summary>
    /// Register plugins to load before AppHost Configure
    /// </summary>
    public static List<IPlugin> DefaultPlugins() =>
    [
        new PreProcessRequest(),
        new Formats.HtmlFormat(),
        new Formats.CsvFormat(),
        new Formats.JsonlFormat(),
        new PredefinedRoutesFeature(),
        new MetadataFeature(),
        new NativeTypesFeature(),
        new HttpCacheFeature(),
        new RequestInfoFeature(),
        new SvgFeature(),
        new UiFeature(),
        new Validation.ValidationFeature(),
        new VirtualFilesFeature(),
    ];

    public List<Assembly> ExcludeServiceAssemblies { get; } = [
        typeof(Service).Assembly,
        typeof(Authenticate).Assembly,
    ];

    /// <summary>
    /// Find All Service Assemblies
    /// </summary>
    public HashSet<Assembly> ResolveAllServiceAssemblies()
    {
        // return empty, if not scanning assemblies
        // if (ServiceAssemblies.Count == 0 && (ServiceStackHost.Instance == null || ServiceStackHost.Instance.ServiceAssemblies.Count == 0))
        //     return [];
        
        var assemblies = new HashSet<Assembly>(ServiceAssemblies);
        ServiceTypes.Each(x => assemblies.Add(x.Assembly));
        ServicesRegistered.Each(x => assemblies.Add(x.Assembly));
        ServiceRoutes.Keys.Each(x => assemblies.Add(x.Assembly));
        if (ServiceStackHost.Instance != null)
        {
            assemblies.AddDistinctRange(ServiceStackHost.Instance.ServiceAssemblies);
        }
        ExcludeServiceAssemblies.ForEach(x => assemblies.Remove(x));
        return assemblies;
    }

    /// <summary>
    /// Find all IService types in Service Assemblies
    /// </summary>
    /// <returns></returns>
    public HashSet<Type> ResolveAssemblyServiceTypes()
    {
        var assemblies = ResolveAllServiceAssemblies();
        var serviceTypes = assemblies.SelectMany(x => x.GetTypes()).Where(x => x.HasInterface(typeof(IService))).ToSet();
        serviceTypes.AddDistinctRange(ServiceTypes);
        return serviceTypes;
    }

    /// <summary>
    /// Find all Request DTO types in Service Assemblies
    /// </summary>
    /// <returns></returns>
    public HashSet<Type> ResolveAssemblyRequestTypes(Func<Type,bool>? include = null)
    {
        var origAssemblies = ResolveAllServiceAssemblies();

        var serviceTypes = ResolveAssemblyServiceTypes();
        var requestTypes = ServiceController.GetServiceRequestTypes(serviceTypes);
        
        if (include == null)
            return requestTypes;
        
        var assemblies = new HashSet<Assembly>(origAssemblies);
        foreach (var requestType in requestTypes)
        {
            assemblies.Add(requestType.Assembly);
        }
        
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(include))
            {
                if (!ServiceController.IsRequestType(type))
                    continue;
                
                requestTypes.Add(type);
            }
        }

        return requestTypes;
    }
    
    /// <summary>
    /// Find all available Request DTOs in GlobalServiceAssemblies, GlobalServices and GlobalServiceRoutes
    /// </summary>
    /// <returns></returns>
    public Dictionary<Type, Type> ResolveRequestServiceTypesMap()
    {
        var to = new Dictionary<Type, Type>();
        // If AppHost has initialized use Metadata Operations
        if (ServiceStackHost.Instance != null)
        {
            foreach (var entry in ServiceStackHost.Instance.Metadata.OperationsMap)
            {
                to[entry.Key] = entry.Value.ServiceType;
            }
            return to;
        }

        // Otherwise use registered Services
        var allServiceTypes = ServiceAssemblies.SelectMany(x => x.GetTypes().Where(ServiceController.IsServiceType)).ToSet();
        allServiceTypes.AddDistinctRange(ServiceTypes);
        allServiceTypes.AddDistinctRange(ServiceRoutes.Keys);

        foreach (var serviceType in allServiceTypes)
        {
            foreach (var action in serviceType.GetActions())
            {
                to[action.RequestType] = serviceType;
            }
        }
        return to;
    }
}

#if NET8_0_OR_GREATER

public class ServiceStackOptions
{
    /// <summary>
    /// Generate ASP.NET Core Endpoints for ServiceStack APIs
    /// </summary>
    public void MapEndpoints(bool use = true, bool force = true, bool useSystemJson = true)
    {
        MapEndpointRouting = true;
        UseEndpointRouting = use;
        DisableServiceStackRouting = force;
        UseSystemJson = useSystemJson;
    }

    /// <summary>
    /// Use ASP .NET Route Endpoint implementations
    /// </summary>
    public bool MapEndpointRouting { get; set; }

    /// <summary>
    /// Use ASP .NET Route Endpoint implementations
    /// </summary>
    public bool UseEndpointRouting { get; set; }

    /// <summary>
    /// Use System.Text JSON for ServiceStack APIs
    /// </summary>
    public bool UseSystemJson { get; set; }

    /// <summary>
    /// The ASP.NET Core AuthenticationSchemes to use for protected ServiceStack APIs
    /// </summary>
    public string AuthenticationSchemes { get; set; } = 
        Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme + ",Bearer,basic";
    
    /// <summary>
    /// Custom handlers to execute for each ServiceStack API endpoint
    /// </summary>
    public List<RouteHandlerBuilderDelegate> RouteHandlerBuilders { get; } = [];
    
    /// <summary>
    /// Whether to disable ServiceStack Routing and use ASP.NET Core Endpoint Routing to handle all ServiceStack Requests
    /// </summary>
    public bool DisableServiceStackRouting { get; set; }
}

#endif