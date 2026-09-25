using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
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
    /// Custom Rest Paths to register
    /// </summary>
    public List<RestPath> Routes { get; set; } = [];

    /// <summary>
    /// Auto Register built-in dependencies when not registered
    /// </summary>
    public List<Type> AutoRegister { get; } = [
        typeof(IAppSettings),
        typeof(IVirtualFiles),
        typeof(IVirtualPathProvider),
        typeof(ICacheClient),
        typeof(ICacheClientAsync),
        typeof(MemoryCacheClient),
        typeof(IMessageFactory),
        typeof(ServiceController),
        typeof(HttpUtils),
        typeof(Auth.IPasswordHasher),
        typeof(JS), // JS.Configure()
#if NET8_0_OR_GREATER
        typeof(AppHostStartup),
#endif        
    ];
    
    internal bool ShouldAutoRegister<T>() => AutoRegister.Contains(typeof(T));

    public List<string> AllowedAuthenticationSchemes { get; } =
    [
        "Bearer", "basic", "Identity.Application"
    ];
    
    internal HashSet<Type> ServicesRegistered = [];

    /// <summary>
    /// List of Plugins to Register
    /// </summary>
    public List<IPlugin> Plugins { get; } = DefaultPlugins();
    internal HashSet<IPlugin> PluginsConfigured = [];

    internal HashSet<Type> GetAllServiceTypes()
    {
        var to = ServiceAssemblies.SelectMany(assembly => assembly.GetTypes()
            .Where(type => ServiceController.IsServiceType(type) && !type.GetCustomAttributes<IgnoreServicesAttribute>().Any())
        ).ToSet();
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

    public Dictionary<Type, List<Action<IPlugin>>> OnPreRegisterPlugins { get; set; } = new();

    public Dictionary<Type, List<Action<IPlugin>>> OnPostRegisterPlugins { get; set; } = new();

    /// <summary>
    /// Exclude Assemblies when Auto Registering ServiceStack Services
    /// </summary>
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
            var metadata = ServiceStackHost.GetOrCreateMetadata();
            foreach (var entry in metadata.OperationsMap)
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
    
    public Type? HostType { get; set; }
}

#if NET8_0_OR_GREATER

public class ServiceStackOptions
{
    /// <summary>
    /// Generate ASP.NET Core Endpoints for ServiceStack APIs
    /// </summary>
    public void MapEndpoints(bool use = true, bool force = true, UseSystemJson useSystemJson = UseSystemJson.Always)
    {
        MapEndpointRouting = true;
        UseEndpointRouting = use;
        DisableServiceStackRouting = force;
        UseSystemJson = useSystemJson;
    }

    private readonly Dictionary<string, string> rateLimitTags = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, string> rateLimitOperations = new();
    private readonly Dictionary<Type, string?> resolvedRateLimits = new();

    /// <summary>Attach a named ASP.NET Core rate-limiting policy to every operation with this tag.</summary>
    public void RateLimitTag(string tag, string policyName)
    {
        ValidateRateLimitName(tag, nameof(tag));
        ValidateRateLimitName(policyName, nameof(policyName));
        if (rateLimitTags.TryGetValue(tag, out var existing) && existing != policyName)
            throw new InvalidOperationException($"Rate-limiting tag '{tag}' is already bound to '{existing}'.");
        rateLimitTags[tag] = policyName;
    }

    /// <summary>Attach a named ASP.NET Core rate-limiting policy to an operation.</summary>
    public void RateLimitOperation<TRequest>(string policyName) => RateLimitOperation(typeof(TRequest), policyName);

    public void RateLimitOperation(Type requestType, string policyName)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ValidateRateLimitName(policyName, nameof(policyName));
        if (rateLimitOperations.TryGetValue(requestType, out var existing) && existing != policyName)
            throw new InvalidOperationException($"Operation '{requestType.Name}' is already bound to '{existing}'.");
        rateLimitOperations[requestType] = policyName;
    }

    private static void ValidateRateLimitName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Rate-limiting names cannot be empty.", parameterName);
    }

    internal bool HasRateLimiting { get; private set; }

    public void ValidateRateLimiting(IEnumerable<Operation> operations)
    {
        foreach (var operation in operations)
            ResolveRateLimiting(operation);
        if (HasRateLimiting && (!MapEndpointRouting || !UseEndpointRouting || !DisableServiceStackRouting))
            throw new InvalidOperationException("Rate-limited ServiceStack operations require options.MapEndpoints(use: true, force: true) to prevent unmetered legacy routes.");
    }

    internal (string? Policy, bool Disabled) ResolveRateLimiting(Operation operation)
    {
        var requestType = operation.RequestType;
        var disabled = requestType.IsDefined(typeof(Microsoft.AspNetCore.RateLimiting.DisableRateLimitingAttribute), true);
        if (resolvedRateLimits.TryGetValue(requestType, out var cached))
            return (cached, disabled);

        var serviceStack = (RateLimitingAttribute?)Attribute.GetCustomAttribute(requestType, typeof(RateLimitingAttribute), true);
        var microsoft = (Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute?)Attribute.GetCustomAttribute(
            requestType, typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute), true);
        if (serviceStack != null) ValidateRateLimitName(serviceStack.PolicyName, nameof(RateLimitingAttribute.PolicyName));
        if (microsoft != null) ValidateRateLimitName(microsoft.PolicyName, nameof(microsoft.PolicyName));
        if (serviceStack != null && microsoft != null && serviceStack.PolicyName != microsoft.PolicyName)
            throw new InvalidOperationException($"Operation '{operation.Name}' has conflicting rate-limiting attributes: '{serviceStack.PolicyName}' and '{microsoft.PolicyName}'.");

        rateLimitOperations.TryGetValue(requestType, out var explicitPolicy);
        var attributePolicy = serviceStack?.PolicyName ?? microsoft?.PolicyName;
        if (explicitPolicy != null && attributePolicy != null && explicitPolicy != attributePolicy)
            throw new InvalidOperationException($"Operation '{operation.Name}' has conflicting rate-limiting bindings: '{explicitPolicy}' and '{attributePolicy}'.");
        if (disabled && (explicitPolicy != null || attributePolicy != null))
            throw new InvalidOperationException($"Operation '{operation.Name}' disables rate limiting but also selects a policy.");

        var policy = explicitPolicy ?? attributePolicy;
        if (policy == null && !disabled)
        {
            var matches = operation.Tags.Where(rateLimitTags.ContainsKey)
                .Select(tag => (Tag: tag, Policy: rateLimitTags[tag])).ToList();
            var distinct = matches.Select(x => x.Policy).Distinct(StringComparer.Ordinal).ToList();
            if (distinct.Count > 1)
                throw new InvalidOperationException($"Operation '{operation.Name}' has conflicting rate-limiting tags: " +
                    string.Join(", ", matches.Select(x => $"{x.Tag}={x.Policy}")));
            policy = distinct.FirstOrDefault();
        }
        HasRateLimiting |= policy != null;
        resolvedRateLimits[requestType] = policy;
        return (policy, disabled);
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
    /// The ASP.NET Core AuthenticationSchemes to use for protected ServiceStack APIs
    /// </summary>
    public string? AuthenticationSchemes { get; set; }
    
    /// <summary>
    /// Custom handlers to execute for each ServiceStack API endpoint
    /// </summary>
    public List<RouteHandlerBuilderDelegate> RouteHandlerBuilders { get; } = [];
    
    /// <summary>
    /// Whether to disable ServiceStack Routing and use ASP.NET Core Endpoint Routing to handle all ServiceStack Requests
    /// </summary>
    public bool DisableServiceStackRouting { get; set; }
    
    /// <summary>
    /// Use System.Text JSON for ServiceStack APIs
    /// </summary>
    public UseSystemJson UseSystemJson { get; set; }

    /// <summary>
    /// Customize System.Text.Json serialization options
    /// </summary>
    public static System.Text.Json.JsonSerializerOptions SystemJsonOptions => Text.TextConfig.SystemJsonOptions;
}

public record struct EndpointOptions(bool RequireAuth=true);

public class AppHostStartup(
    Microsoft.Extensions.Logging.ILogger<AppHostStartup> log, 
    Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime) 
    : Microsoft.Extensions.Hosting.IHostedService
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LoadedPlugins { get; set; }
        
    public async System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken token)
    {
        StartedAt = DateTime.UtcNow;
        
        var loadPlugins = ServiceStackHost.Instance.Plugins.OfType<IRequireLoadAsync>().ToList();
        Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(log, "Loading {Count} async plugins", loadPlugins.Count);
        foreach (var plugin in loadPlugins)
        {
            var pluginStart = DateTime.UtcNow;
            try
            {
                await plugin.LoadAsync(token).ConfigureAwait(false);
                Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(log, "Loaded {Plugin} in {Duration}", 
                    plugin.GetType().Name, (DateTime.UtcNow - pluginStart));
                LoadedPlugins++;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(log, e, "Error loading {Plugin}", plugin.GetType().Name);
                appLifetime.StopApplication();
            }
        }

        CompletedAt = DateTime.UtcNow;
        ServiceStackHost.HasLoaded = true;
    }

    public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)
    {
        Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(log, "Loaded {Count} async plugins in {Duration}", 
            LoadedPlugins, (CompletedAt ?? DateTime.UtcNow) - StartedAt);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}    

#endif