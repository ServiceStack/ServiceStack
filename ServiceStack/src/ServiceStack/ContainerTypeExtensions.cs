using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using ServiceStack.Web;

namespace ServiceStack;

#if !NETFRAMEWORK        
public interface IHasServiceScope : IServiceProvider
{
    Microsoft.Extensions.DependencyInjection.IServiceScope ServiceScope { get; set; }
}

public static class ServiceScopeExtensions
{
    public static Microsoft.Extensions.DependencyInjection.IServiceScope StartScope(this IRequest request)
    {
        if (request is IHasServiceScope hasScope)
        {
            var scopeFactory = (Microsoft.Extensions.DependencyInjection.IServiceScopeFactory) 
                hasScope.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory));
            if (scopeFactory == null)
                return null;
            
            var scope = scopeFactory.CreateScope();
            hasScope.ServiceScope = scope;
            return scope;
        }
        return null;
    }
}
#endif

public static class ContainerTypeExtensions
{
    /// <summary>
    /// Registers the type in the IoC container and
    /// adds auto-wiring to the specified type.
    /// </summary>
    public static void RegisterAutoWiredType(this Container container, Type serviceType, Type inFunqAsType,
        ReuseScope scope = ReuseScope.None)
    {
        if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
            return;

        var methodInfo = typeof(Container).GetMethodInfo(nameof(Container.RegisterAutoWiredAs), Type.EmptyTypes);
        var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType, inFunqAsType });

        var registration = registerMethodInfo.Invoke(container, TypeConstants.EmptyObjectArray) as IRegistration;
        registration.ReusedWithin(scope);
    }

    /// <summary>
    /// Registers a named instance of type in the IoC container and
    /// adds auto-wiring to the specified type.
    /// </summary>
    public static void RegisterAutoWiredType(this Container container, string name, Type serviceType, Type inFunqAsType,
        ReuseScope scope = ReuseScope.None)
    {
        if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
            return;

        var methodInfo = typeof(Container).GetMethodInfo("RegisterAutoWiredAs", new[] { typeof(string) });
        var registerMethodInfo = methodInfo.MakeGenericMethod(serviceType, inFunqAsType);

        var registration = registerMethodInfo.Invoke(container, new[] { name }) as IRegistration;
        registration.ReusedWithin(scope);
    }

    /// <summary>
    /// Registers the type in the IoC container and
    /// adds auto-wiring to the specified type.
    /// The reuse scope is set to none (transient).
    /// </summary>
    public static void RegisterAutoWiredType(this Container container, Type serviceType,
        ReuseScope scope = ReuseScope.None)
    {
        //Don't try to register base service classes
        if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
            return;

        var methodInfo = typeof(Container).GetMethodInfo("RegisterAutoWired", Type.EmptyTypes);
        var registerMethodInfo = methodInfo.MakeGenericMethod(serviceType);

        var registration = registerMethodInfo.Invoke(container, TypeConstants.EmptyObjectArray) as IRegistration;
        registration.ReusedWithin(scope);
    }

    /// <summary>
    /// Registers the type in the IoC container and
    /// adds auto-wiring to the specified type.
    /// The reuse scope is set to none (transient).
    /// </summary>
    public static void RegisterAutoWiredType(this Container container, string name, Type serviceType,
        ReuseScope scope = ReuseScope.None)
    {
        //Don't try to register base service classes
        if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
            return;

        var methodInfo = typeof(Container).GetMethodInfo("RegisterAutoWired", new[] { typeof(string) });
        var registerMethodInfo = methodInfo.MakeGenericMethod(serviceType);

        var registration = registerMethodInfo.Invoke(container, new[] { name }) as IRegistration;
        registration.ReusedWithin(scope);
    }

    /// <summary>
    /// Registers the types in the IoC container and
    /// adds auto-wiring to the specified types.
    /// The reuse scope is set to none (transient).
    /// </summary>
    public static void RegisterAutoWiredTypes(this Container container, IEnumerable<Type> serviceTypes,
        ReuseScope scope = ReuseScope.None)
    {
        foreach (var serviceType in serviceTypes)
            container.RegisterAutoWiredType(serviceType, scope);
    }

    /// <summary>
    /// Register a singleton instance as a runtime type
    /// </summary>
    public static Container Register(this Container container, object instance, Type asType)
    {
        var mi = container.GetType()
            .GetMethods()
            .First(x => x.Name == "Register" && x.GetParameters().Length == 1 && x.ReturnType == typeof(void))
            .MakeGenericMethod(asType);

        mi.Invoke(container, new[] { instance });
        return container;
    }
}