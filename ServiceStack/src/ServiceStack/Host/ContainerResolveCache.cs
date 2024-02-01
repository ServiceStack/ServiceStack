using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using Funq;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.Host;

public class ContainerResolveCache : ITypeFactory
{
    static readonly ConcurrentDictionary<Type, Func<IResolver, object>> resolveFnMap = new();

    private Func<IResolver, object> GenerateServiceFactory(Type type)
    {
        var resolverParam = Expression.Parameter(typeof(IResolver), "resolver");
        var resolveInstance = Expression.Call(resolverParam, "TryResolve", [type]);
        var resolveObject = Expression.Convert(resolveInstance, typeof(object));
#if NET8_0_OR_GREATER
        if (type.HasInterface(typeof(IService)) && ServiceStackHost.InitOptions.RegisterServicesInServiceCollection)
        {
            var populateInstance = Expression.Call(null, typeof(ContainerResolveCache)
                .GetMethod(nameof(PopulateInstance), BindingFlags.Public | BindingFlags.Static)!, 
                [resolverParam,resolveObject]);
            return Expression.Lambda<Func<IResolver, object>>(populateInstance, resolverParam).Compile();
        }
#endif
        return Expression.Lambda<Func<IResolver, object>>(resolveObject, resolverParam).Compile();
    }
    
#if NET8_0_OR_GREATER
    static readonly ConcurrentDictionary<Type, Action<IResolver,object>[]> settersCache = new();
    
    public static object PopulateInstance(IResolver resolver, object instance)
    {
        var setters = settersCache.GetOrAdd(instance.GetType(), serviceType => {
            var setters = new List<Action<IResolver,object>>();
            foreach (var prop in serviceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite && x.HasAttribute<Microsoft.AspNetCore.Mvc.FromServicesAttribute>()))
            {
                var setter = prop.GetSetMethod();
                if (setter == null) continue;
                
                var resolverParam = Expression.Parameter(typeof(IResolver), "resolver");
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var resolveInstance = Expression.Call(resolverParam, "TryResolve", [prop.PropertyType]);
                var setterCall = Expression.Call(Expression.Convert(instanceParam, serviceType), setter, resolveInstance);
                var setterLambda = Expression.Lambda<Action<IResolver,object>>(setterCall, resolverParam, instanceParam).Compile();
                setters.Add(setterLambda);
            }
            return setters.ToArray();
        });

        foreach (var setter in setters)
        {
            setter(resolver, instance);
        }
        
        return instance;
    }
#endif

    /// <summary>
    /// Creates instance using straight Resolve approach.
    /// This will throw an exception if resolution fails
    /// </summary>
    public object CreateInstance(IResolver resolver, Type type)
    {
        return CreateInstance(resolver, type, false);
    }

    /// <summary>
    /// Creates instance using the TryResolve approach if tryResolve = true.
    /// Otherwise uses Resolve approach, which will throw an exception if resolution fails
    /// </summary>
	public object CreateInstance(IResolver resolver, Type type, bool tryResolve)
    {
        var resolveFn = resolveFnMap.GetOrAdd(type, GenerateServiceFactory(type));

        var instance = resolveFn(resolver);
        if (instance == null && !tryResolve)
            throw new ResolutionException(type);

        return instance;
    }

    public static void Reset()
    {
        resolveFnMap.Clear();
#if NET8_0_OR_GREATER
        settersCache.Clear();
#endif
    }
}
