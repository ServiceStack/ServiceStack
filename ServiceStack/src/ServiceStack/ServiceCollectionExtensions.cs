#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Host;

namespace ServiceStack;

/// <summary>
/// Register Plugin Dependencies in IOC 
/// </summary>
public interface IConfigureServices 
{
    void Configure(IServiceCollection services);
}
    
/// <summary>
/// Register Plugin Dependencies in IOC after all Configure(services) have been run 
/// </summary>
public interface IPostConfigureServices 
{
    public int Priority { get; }
    
    void AfterConfigure(IServiceCollection services);
}

public static class ConfigurePriority
{
    public const int AutoQueryDataFeature = 10;
    public const int AutoQueryFeature = 20;
    public const int ValidationFeature = 100;
}

public static class ServiceCollectionExtensions
{
    public static bool Exists<TService>(this IServiceCollection services) => Exists(services, typeof(TService));

    public static bool Exists(this IServiceCollection services, Type serviceType)
    {
        if (services is Funq.Container container)
            return container.Exists(serviceType);

        return services.Any(x => x.ServiceType == serviceType);
    }

    public static ServiceLifetime ToServiceLifetime(this ReuseScope scope) => scope switch
    {
        ReuseScope.None => ServiceLifetime.Transient,
        ReuseScope.Request => ServiceLifetime.Scoped,
        _ => ServiceLifetime.Singleton
    };

    public static ReuseScope ToReuseScope(this ServiceLifetime lifetime) => lifetime switch
    {
        ServiceLifetime.Transient => ReuseScope.None,
        ServiceLifetime.Scoped=> ReuseScope.Request,
        _ => ReuseScope.Container
    };

    public static IServiceCollection Add(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return services;
    }

    public static IServiceCollection Add(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    {
        services.Add(new ServiceDescriptor(serviceType, factory, lifetime));
        return services;
    }

    private static void AssertServiceType(Type serviceType)
    {
        if (!ServiceController.IsServiceType(serviceType))
            throw new NotSupportedException($"{serviceType.Name} is not a ServiceStack Service");
    }

    public static void RegisterServices(this IServiceCollection services, Dictionary<Type, string[]> serviceRoutes)
    {
        foreach (var entry in serviceRoutes)
        {
            AssertServiceType(entry.Key);
            
            ServiceStackHost.InitOptions.ServiceRoutes[entry.Key] = entry.Value;
        }
    }

    public static void RegisterService<T>(this IServiceCollection services) where T : IService =>
        services.RegisterService(typeof(T));
    
    public static void RegisterService(this IServiceCollection services, Type serviceType)
    {
        AssertServiceType(serviceType);
        ServiceStackHost.InitOptions.ServiceTypes.AddIfNotExists(serviceType);
    }

    public static void RegisterService(this IServiceCollection services, Type serviceType, string route)
    {
        AssertServiceType(serviceType);
        ServiceStackHost.InitOptions.ServiceRoutes[serviceType] = [route];
    }

    public static void RegisterService(this IServiceCollection services, Type serviceType, string[] routes)
    {
        AssertServiceType(serviceType);
        ServiceStackHost.InitOptions.ServiceRoutes[serviceType] = routes;
    }
}
