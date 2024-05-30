using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class AppHostExtensions
{
    private static readonly ILog log = LogManager.GetLogger(typeof(AppHostExtensions));

    public static void ConfigureOperation<T>(this IAppHost appHost, Action<Operation> configure)
    {
        appHost.ConfigureOperations(op => {
            if (op.RequestType != typeof(T)) return;
            configure(op);
        });
    }

    public static void ConfigureOperations(this IAppHost appHost, Action<Operation> configure) => 
        appHost.Metadata.ConfigureOperations.Add(configure);

    public static void ConfigureType<T>(this IAppHost appHost, Action<MetadataType> configure)
    {
        appHost.ConfigureTypes(metaType => {
            if (metaType.Type != null)
            {
                if (metaType.Type == typeof(T)) 
                    configure(metaType);
            }
            else if ((metaType.Namespace == null || metaType.Namespace == typeof(T).Namespace)
                     && metaType.Name == typeof(T).Name)
            {
                configure(metaType);
            }
        });
    }

    public static void ConfigureTypes(this IAppHost appHost, Action<MetadataType> configure) => 
        appHost.Metadata.ConfigureMetadataTypes.Add(configure);
    public static void ConfigureTypes(this IAppHost appHost, Action<MetadataType> configure, Predicate<MetadataType> where) => 
        appHost.Metadata.ConfigureMetadataTypes.Add(type => {
            if (where(type))
                configure(type);
        });

    public static void RegisterServices(this IAppHost appHost, Dictionary<Type, string[]> serviceRoutes)
    {
        if (serviceRoutes == null)
            return;
        
        foreach (var registerService in serviceRoutes)
        {
            appHost.RegisterService(registerService.Key, registerService.Value);
        }
    }

    public static Dictionary<Type, string[]> RemoveService<T>(this Dictionary<Type, string[]> serviceRoutes)
    {
        serviceRoutes?.TryRemove(typeof(T), out _);
        return serviceRoutes;
    }

    public static void RegisterService<TService>(this IAppHost appHost, params string[] atRestPaths)
    {
        appHost.RegisterService(typeof(TService), atRestPaths);
    }

    public static void RegisterRequestBinder<TRequest>(this IAppHost appHost, Func<IRequest, object> binder)
    {
        appHost.RequestBinders[typeof(TRequest)] = binder;
    }

    public static void AddPluginsFromAssembly(this IAppHost appHost, params Assembly[] assembliesWithPlugins)
    {
        var ssHost = (ServiceStackHost)appHost;
        foreach (var assembly in assembliesWithPlugins)
        {
            var pluginTypes =
                from t in assembly.GetExportedTypes()
                where t.GetInterfaces().Any(x => x == typeof(IPlugin))
                select t;

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    if (pluginType.CreateInstance() is IPlugin plugin)
                    {
                        ssHost.LoadPlugin(plugin);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error adding new Plugin " + pluginType.GetOperationName(), ex);
                }
            }
        }
    }

    public static T GetPlugin<T>(this IAppHost appHost) where T : class, IPlugin
    {
        return appHost.Plugins.FirstOrDefault(x => x is T) as T;
    }

    public static T AssertPlugin<T>(this IAppHost appHost) where T : class, IPlugin
    {
        var plugin = appHost.Plugins.FirstOrDefault(x => x is T) as T;
        if (plugin == null)
            throw new NotSupportedException($"Required Plugin '{typeof(T).Name}' was not registered");

        return plugin;
    }

    public static bool HasPlugin<T>(this IAppHost appHost) where T : class, IPlugin
    {
        return appHost.Plugins.FirstOrDefault(x => x is T) != null;
    }

    public static bool HasMultiplePlugins<T>(this IAppHost appHost) where T : class, IPlugin
    {
        return appHost.Plugins.Count(x => x is T) > 1;
    }

    /// <summary>
    /// Get an IAppHost container. 
    /// Note: Registering dependencies should only be done during setup/configuration 
    /// stage and remain immutable there after for thread-safety.
    /// </summary>
    /// <param name="appHost"></param>
    /// <returns></returns>
    public static Container GetContainer(this IAppHost appHost)
    {
        var hasContainer = appHost as IHasContainer;
        return hasContainer?.Container;
    }

    public static bool NotifyStartupException(this IAppHost appHost, Exception ex, string target, string method)
    {
        var ssHost = HostContext.AppHost;
        if (ssHost == null) return false;

        if (!ssHost.HasStarted)
        {
            ssHost.OnStartupException(ex, target, method);
        }
        return !ssHost.HasStarted;
    }

    public static bool NotifyStartupException(this IAppHost appHost, Exception ex)
    {
        var ssHost = HostContext.AppHost;
        if (ssHost == null) return false;

        if (!ssHost.HasStarted)
        {
            ssHost.OnStartupException(ex);
        }
        return !ssHost.HasStarted;
    }

    public static string Localize(this string text, IRequest request=null) => 
        HostContext.AppHost?.ResolveLocalizedString(text, request) ?? text;

    public static string LocalizeFmt(this string text, IRequest request, params object[] args) => 
        HostContext.AppHost?.ResolveLocalizedStringFormat(text, args, request) ?? string.Format(text, args);
    public static string LocalizeFmt(this string text, params object[] args) => 
        HostContext.AppHost?.ResolveLocalizedStringFormat(text, args, request:null) ?? string.Format(text, args);

    public static IAppHost Start(this IAppHost appHost, IEnumerable<string> urlBases)
    {
#if NETFRAMEWORK
        var listener = (ServiceStack.Host.HttpListener.HttpListenerBase)appHost;
        listener.Start(urlBases);
#endif
        return appHost;
    }

    public static List<IPlugin> AddIfDebug<T>(this List<IPlugin> plugins, T plugin) where T : class, IPlugin
    {
        if (HostContext.DebugMode)
            plugins.Add(plugin);
        return plugins;
    }

    public static List<IPlugin> AddIfNotExists<T>(this List<IPlugin> plugins, T plugin) where T : class, IPlugin =>
        plugins.AddIfNotExists(plugin, null);
    public static List<IPlugin> AddIfNotExists<T>(this List<IPlugin> plugins, T plugin, Action<T> configure) where T : class, IPlugin
    {
        var existingPlugin = plugins.FirstOrDefault(x => x is T) as T; 
        if (existingPlugin == null)
            plugins.Add(plugin);

        configure?.Invoke(existingPlugin ?? plugin);

        return plugins;
    }

    public static string ResolveStaticBaseUrl(this IAppHost appHost)
    {
        return (appHost.Config.WebHostUrl ??
            (!string.IsNullOrEmpty(appHost.PathBase)
                ? ("/" + appHost.PathBase).Replace("//", "/")
                : "")).TrimEnd('/');
    }

    public static bool IsRunAsAppTask(this IAppHost appHost) => AppTasks.IsRunAsAppTask();

    public static string GetOperationName(this ServiceStack.Host.Handlers.IServiceStackHandler handler)
    {
        return handler.RequestName;
    }
}
