using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using ServiceStack;
using ServiceStack.Configuration;
using System;

namespace Funq;

public partial class Container : IResolver, IContainer
{
    public IContainerAdapter Adapter { get; set; }

    /// <summary>
    /// Register an autowired dependency
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public IRegistration<T> RegisterAutoWired<T>()
    {
        var serviceFactory = GenerateAutoWireFn<T>();
        return this.Register(serviceFactory);
    }

    /// <summary>
    /// Register an autowired dependency
    /// </summary>
    /// <param name="name">Name of dependency</param>
    /// <typeparam name="T"></typeparam>
    public IRegistration<T> RegisterAutoWired<T>(string name)
    {
        var serviceFactory = GenerateAutoWireFn<T>();
        return this.Register(name, serviceFactory);
    }

    /// <summary>
    /// Register an autowired dependency as a separate type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TAs"></typeparam>
    public IRegistration<TAs> RegisterAutoWiredAs<T, TAs>()
        where T : TAs
    {
        var serviceFactory = GenerateAutoWireFn<T>();
        Func<Container, TAs> fn = c => serviceFactory(c);
        return this.Register(fn);
    }

    /// <summary>
    /// Register an autowired dependency as a separate type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TAs"></typeparam>
    public IRegistration<TAs> RegisterAutoWiredAs<T, TAs>(string name)
        where T : TAs
    {
        var serviceFactory = GenerateAutoWireFn<T>();
        Func<Container, TAs> fn = c => serviceFactory(c);
        return this.Register(name, fn);
    }

    /// <summary>
    /// Alias for RegisterAutoWiredAs
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TAs"></typeparam>
    public IRegistration<TAs> RegisterAs<T, TAs>()
        where T : TAs
    {
        return this.RegisterAutoWiredAs<T, TAs>();
    }

    /// <summary>
    /// Alias for RegisterAutoWiredAs
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TAs"></typeparam>
    public IRegistration<TAs> RegisterAs<T, TAs>(string name)
        where T : TAs
    {
        return this.RegisterAutoWiredAs<T, TAs>(name);
    }

    /// <summary>
    /// Auto-wires an existing instance, 
    /// ie all public properties are tried to be resolved.
    /// </summary>
    /// <param name="instance"></param>
    public void AutoWire(object instance)
    {
        AutoWire(this, instance);
    }

    public object GetLazyResolver(params Type[] types) // returns Func<type>
    {
        var tryResolveGeneric = typeof(Container).GetMethods()
            .First(x => x.Name == "ReverseLazyResolve"
                        && x.GetGenericArguments().Length == types.Length
                        && x.GetParameters().Length == 0);

        var tryResolveMethod = tryResolveGeneric.MakeGenericMethod(types);
        var instance = tryResolveMethod.Invoke(this, TypeConstants.EmptyObjectArray);
        return instance;
    }

    public Func<TService> ReverseLazyResolve<TService>()
    {
        return LazyResolve<TService>(null);
    }

    public Func<TArg, TService> ReverseLazyResolve<TArg, TService>()
    {
        Register<Func<TArg, TService>>(c => a => c.TryResolve<TService>());
        return TryResolve<Func<TArg, TService>>();
    }

    public Func<TArg1, TArg2, TService> ReverseLazyResolve<TArg1, TArg2, TService>()
    {
        Register<Func<TArg1, TArg2, TService>>(c => (a1, a2) => c.TryResolve<TService>());
        return TryResolve<Func<TArg1, TArg2, TService>>();
    }

    public Func<TArg1, TArg2, TArg3, TService> ReverseLazyResolve<TArg1, TArg2, TArg3, TService>()
    {
        Register<Func<TArg1, TArg2, TArg3, TService>>(c => (a1, a2, a3) => c.TryResolve<TService>());
        return TryResolve<Func<TArg1, TArg2, TArg3, TService>>();
    }
        
    public ServiceEntry<TService, Func<Container, TService>> GetServiceEntry<TService>() => 
        GetEntry<TService, Func<Container, TService>>(null, throwIfMissing: false);

    public ServiceEntry<TService, Func<Container, TService>> GetServiceEntryNamed<TService>(string name) => 
        GetEntry<TService, Func<Container, TService>>(name, throwIfMissing: false);

    public bool Exists<TService>()
    {
        var entry = GetEntry<TService, Func<Container, TService>>(null, throwIfMissing: false);
        return entry != null;
    }
    public bool ExistsNamed<TService>(string name)
    {
        var entry = GetEntry<TService, Func<Container, TService>>(name, throwIfMissing: false);
        return entry != null;
    }

    public bool Exists(Type type)
    {
        var existsGeneric = typeof(Container).GetMethods()
            .First(x => x.Name == "Exists"
                        && x.IsGenericMethod
                        && x.GetGenericArguments().Length == 1);

        var existsMethod = existsGeneric.MakeGenericMethod(type);
        var instance = existsMethod.Invoke(this, TypeConstants.EmptyObjectArray);
        return instance is bool exists && exists;
    }

    private Dictionary<Type, Action<object>[]> autoWireCache = new();

    private static MethodInfo GetResolveMethod(Type typeWithResolveMethod, Type serviceType)
    {
        var methodInfo = typeWithResolveMethod.GetMethod("Resolve", TypeConstants.EmptyTypeArray);
        return methodInfo.MakeGenericMethod(new[] { serviceType });
    }

    public static ConstructorInfo GetConstructorWithMostParams(Type type)
    {
        return type.GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault(ctor => !ctor.IsStatic);
    }

    public static HashSet<string> IgnorePropertyTypeFullNames = new HashSet<string>
    {
        "System.Web.Mvc.ViewDataDictionary", //overrides ViewBag set in Controller constructor
    };

    private static bool IsPublicWritableUserPropertyType(PropertyInfo pi)
    {
        return pi.CanWrite
               && !pi.PropertyType.IsValueType
               && pi.PropertyType != typeof(string)
               && !IgnorePropertyTypeFullNames.Contains(pi.PropertyType.FullName);
    }

    /// <summary>
    /// Generates a function which creates and auto-wires TService.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public static Func<Container, TService> GenerateAutoWireFn<TService>()
    {
        var lambdaParam = Expression.Parameter(typeof(Container), "container");
        var propertyResolveFn = typeof(Container).GetMethod("TryResolve", TypeConstants.EmptyTypeArray);
        var memberBindings = typeof(TService).GetPublicProperties()
            .Where(IsPublicWritableUserPropertyType)
            .Select(x =>
                Expression.Bind
                (
                    x,
                    ResolveTypeExpression(propertyResolveFn, x.PropertyType, lambdaParam)
                )
            ).ToArray();

        var ctorResolveFn = typeof(Container).GetMethod("Resolve", TypeConstants.EmptyTypeArray);
        return Expression.Lambda<Func<Container, TService>>
        (
            Expression.MemberInit
            (
                ConstructorExpression(ctorResolveFn, typeof(TService), lambdaParam),
                memberBindings
            ),
            lambdaParam
        ).Compile();
    }

    /// <summary>
    /// Auto-wires an existing instance of a specific type.
    /// The auto-wiring progress is also cached to be faster 
    /// when calling next time with the same type.
    /// </summary>
    public void AutoWire(Container container, object instance)
    {
        var instanceType = instance.GetType();
        var propertyResolveFn = typeof(Container).GetMethod("TryResolve", TypeConstants.EmptyTypeArray);

        Action<object>[] setters;
        if (!autoWireCache.TryGetValue(instanceType, out setters))
        {
            setters = instanceType.GetPublicProperties()
                .Where(IsPublicWritableUserPropertyType)
                .Select(x => GenerateAutoWireFnForProperty(container, propertyResolveFn, x, instanceType))
                .ToArray();

            //Support for multiple threads is needed
            Dictionary<Type, Action<object>[]> snapshot, newCache;
            do
            {
                snapshot = autoWireCache;
                newCache = new Dictionary<Type, Action<object>[]>(autoWireCache) {
                    [instanceType] = setters
                };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref autoWireCache, newCache, snapshot), snapshot));
        }

        foreach (var setter in setters)
            setter(instance);
    }

    private static Action<object> GenerateAutoWireFnForProperty(
        Container container, MethodInfo propertyResolveFn, PropertyInfo property, Type instanceType)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var containerParam = Expression.Constant(container);

        Func<object, object> getter = Expression.Lambda<Func<object, object>>(
            Expression.Call(
                Expression.Convert(instanceParam, instanceType),
                property.GetGetMethod()
            ),
            instanceParam
        ).Compile();

        Action<object> setter = Expression.Lambda<Action<object>>(
            Expression.Call(
                Expression.Convert(instanceParam, instanceType),
                property.GetSetMethod(),
                ResolveTypeExpression(propertyResolveFn, property.PropertyType, containerParam)
            ),
            instanceParam
        ).Compile();

        return obj =>
        {
            if (getter(obj) == null) setter(obj);
        };
    }

    public static NewExpression ConstructorExpression(
        MethodInfo resolveMethodInfo, Type type, Expression lambdaParam)
    {
        var ctorWithMostParameters = GetConstructorWithMostParams(type);
        if (ctorWithMostParameters == null)
            throw new Exception(ErrorMessages.ConstructorNotFoundForType.LocalizeFmt(type.Name));

        var constructorParameterInfos = ctorWithMostParameters.GetParameters();
        var regParams = constructorParameterInfos
            .Select(pi => ResolveTypeExpression(resolveMethodInfo, pi.ParameterType, lambdaParam));

        return Expression.New(ctorWithMostParameters, regParams.ToArray());
    }

    private static MethodCallExpression ResolveTypeExpression(
        MethodInfo resolveFn, Type resolveType, Expression containerParam)
    {
        var method = resolveFn.MakeGenericMethod(resolveType);
        return Expression.Call(containerParam, method);
    }

    private static Dictionary<Type, Func<Container,object>> tryResolveCache = new();

    public object TryResolve(Type type)
    {
        if (tryResolveCache.TryGetValue(type, out var fn))
            return fn(this);

        var mi = typeof(Container).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.Name == "TryResolve" &&
                        x.GetGenericArguments().Length == 1 &&
                        x.GetParameters().Length == 0);

        var genericMi = mi.MakeGenericMethod(type);

        var p = Expression.Parameter(typeof(Container), "container");
        fn = Expression.Lambda<Func<Container, object>>(
            Expression.Call(p, genericMi),
            p
        ).Compile();

        Dictionary<Type, Func<Container, object>> snapshot, newCache;
        do
        {
            snapshot = tryResolveCache;
            newCache = new Dictionary<Type, Func<Container, object>>(tryResolveCache) {
                [type] = fn
            };
        } while (!ReferenceEquals(
                     Interlocked.CompareExchange(ref tryResolveCache, newCache, snapshot), snapshot));

        return fn(this);
    }

    public object RequiredResolve(Type type, Type ownerType)
    {
        var instance = Resolve(type);
        if (instance == null)
            throw new ArgumentNullException($"Required Type of '{type.Name}' in '{ownerType.Name}' constructor was not registered in '{GetType().Name}'");

        return instance;
    }

    private static Type[] TryResolveArgs = [typeof(Type)];

    public Func<object> CreateFactory(Type type)
    {
        var containerParam = Expression.Constant(this);
        var memberBindings = type.GetPublicProperties()
            .Where(IsPublicWritableUserPropertyType)
            .Select(x =>
                Expression.Bind
                (
                    x,
                    Expression.TypeAs(Expression.Call(containerParam, GetType().GetMethodInfo(nameof(TryResolve), TryResolveArgs), Expression.Constant(x.PropertyType)), x.PropertyType)
                )
            ).ToArray();

        var ctorWithMostParameters = GetConstructorWithMostParams(type);
        if (ctorWithMostParameters == null)
            throw new Exception($"Constructor not found for Type '{type.Name}");

        var constructorParameterInfos = ctorWithMostParameters.GetParameters();
        var regParams = constructorParameterInfos
            .Select(x => 
                Expression.TypeAs(Expression.Call(containerParam, GetType().GetMethodInfo(nameof(RequiredResolve)), Expression.Constant(x.ParameterType), Expression.Constant(type)), x.ParameterType)
            );

        return Expression.Lambda<Func<object>>
        (
            Expression.TypeAs(Expression.MemberInit
            (
                Expression.New(ctorWithMostParameters, regParams.ToArray()),
                memberBindings
            ), typeof(object))
        ).Compile();
    }

    public IRegistration<TService> RegisterFactory<TService>(Func<object> factory)
    {
        return Register(null, c => (TService)factory());
    }
        
    public IContainer AddSingleton(Type serviceType, Func<object> factory)
    {
        var methodInfo = typeof(Container).GetMethodInfo(nameof(RegisterFactory));
        var registerMethodInfo = methodInfo.MakeGenericMethod(serviceType);
        var registration = (IRegistration)registerMethodInfo.Invoke(this, [factory]);
        registration.ReusedWithin(ReuseScope.Container);
        return this;
    }

    public IContainer AddTransient(Type serviceType, Func<object> factory)
    {
        var methodInfo = typeof(Container).GetMethodInfo(nameof(RegisterFactory));
        var registerMethodInfo = methodInfo.MakeGenericMethod(serviceType);
        var registration = (IRegistration)registerMethodInfo.Invoke(this, [factory]);
        registration.ReusedWithin(ReuseScope.None);
        return this;
    }

    public object Resolve(Type type)
    {
        return TryResolve(type);
    }
}