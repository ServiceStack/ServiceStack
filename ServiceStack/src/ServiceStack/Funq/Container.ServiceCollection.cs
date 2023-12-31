using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

namespace Funq;

[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(ServiceCollectionDebugView))]
public partial class Container : IServiceCollection
{
    public IRegistration<TService> RegisterServiceProviderFactory<TService>(Func<IServiceProvider, object> factory)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        return Register(null, c =>
        {
            var result = factory(c);
            return (TService)result;
        });
    }
    
    public Func<IServiceProvider, object> CreateFactory(ServiceDescriptor item)
    {
        var factory = item!.ImplementationFactory
                      ?? (item.ImplementationType != null
                          ? CreateServiceCollectionFactory(item.ImplementationType)
                          : c => item.ImplementationInstance);
        return factory;
    }

    public Func<IServiceProvider, object> CreateServiceCollectionFactory(Type type)
    {
        var containerParam = Expression.Constant(this);
        var memberBindings = type.GetPublicProperties()
            .Where(IsPublicWritableUserPropertyType)
            .Select(x =>
                Expression.Bind
                (
                    x,
                    Expression.TypeAs(Expression.Call(
                        containerParam, 
                        GetType().GetMethodInfo(nameof(TryResolve), TryResolveArgs), 
                        Expression.Constant(x.PropertyType)), 
                        x.PropertyType)
                )
            ).ToArray();

        var ctorWithMostParameters = GetConstructorWithMostParams(type);
        if (ctorWithMostParameters == null)
            throw new Exception($"Constructor not found for Type '{type.Name}");

        var constructorParameterInfos = ctorWithMostParameters.GetParameters();
        var regParams = constructorParameterInfos
            .Select(x => 
                Expression.TypeAs(Expression.Call(
                    containerParam, 
                    GetType().GetMethodInfo(nameof(RequiredResolve)), 
                    Expression.Constant(x.ParameterType), 
                    Expression.Constant(type)), 
                    x.ParameterType)
            );

        return Expression.Lambda<Func<IServiceProvider, object>>
        (
            Expression.TypeAs(Expression.MemberInit
            (
                Expression.New(ctorWithMostParameters, regParams.ToArray()),
                memberBindings
            ), typeof(object)),
            Expression.Parameter(typeof(IServiceProvider), "oInstanceParam")
        ).Compile();
    }

    public IServiceCollection AddSingleton(Type serviceType, Func<IServiceProvider, object> factory) => 
        Add(serviceType, factory, ServiceLifetime.Singleton);

    public IServiceCollection AddTransient(Type serviceType, Func<IServiceProvider, object> factory) => 
        Add(serviceType, factory, ServiceLifetime.Transient);

    public IServiceCollection AddScoped(Type serviceType, Func<IServiceProvider, object> factory) => 
        Add(serviceType, factory, ServiceLifetime.Scoped);
    
    public IServiceCollection Add(Type serviceType, Type implementationType, ServiceLifetime lifetime) => 
        Add(serviceType, CreateServiceCollectionFactory(implementationType), lifetime);

    public IServiceCollection Add(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    {
        var methodInfo = typeof(Container).GetMethodInfo(nameof(RegisterServiceProviderFactory));
        var registerMethodInfo = methodInfo.MakeGenericMethod(serviceType);
        var registration = (IRegistration)registerMethodInfo.Invoke(this, [factory]);
        registration.ReusedWithin(lifetime.ToReuseScope());
        return this;
    }

    public void Add(ServiceDescriptor item)
    {
        CheckReadOnly();
        
        var factory = CreateFactory(item);
        
        Add(item.ServiceType, factory, item.Lifetime);
        descriptors.Add(item);
    }

    private readonly List<ServiceDescriptor> descriptors = [];
    private bool isReadOnly;

    /// <inheritdoc />
    public int Count => descriptors.Count;

    /// <inheritdoc />
    public bool IsReadOnly => isReadOnly;

    /// <inheritdoc />
    public ServiceDescriptor this[int index]
    {
        get => descriptors[index];
        set
        {
            CheckReadOnly();
            descriptors[index] = value;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        CheckReadOnly();
        descriptors.Clear();
    }

    /// <inheritdoc />
    public bool Contains(ServiceDescriptor item)
    {
        return descriptors.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        descriptors.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(ServiceDescriptor item)
    {
        CheckReadOnly();
        return descriptors.Remove(item);
    }

    /// <inheritdoc />
    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        return descriptors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int IndexOf(ServiceDescriptor item)
    {
        return descriptors.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, ServiceDescriptor item)
    {
        CheckReadOnly();
        descriptors.Insert(index, item);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        CheckReadOnly();
        descriptors.RemoveAt(index);
    }

    /// <summary>
    /// Makes this collection read-only.
    /// </summary>
    /// <remarks>
    /// After the collection is marked as read-only, any further attempt to modify it throws an <see cref="InvalidOperationException" />.
    /// </remarks>
    public void MakeReadOnly()
    {
        isReadOnly = true;
    }

    private void CheckReadOnly()
    {
        if (isReadOnly)
        {
            ThrowReadOnlyException();
        }
    }

    private static void ThrowReadOnlyException() =>
        throw new InvalidOperationException("ServiceCollection is Read Only");

    private string DebuggerToString()
    {
        var debugText = $"Count = {descriptors.Count}";
        if (isReadOnly)
        {
            debugText += $", IsReadOnly = true";
        }

        return debugText;
    }

    private sealed class ServiceCollectionDebugView(ServiceCollection services)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ServiceDescriptor[] Items
        {
            get
            {
                var items = new ServiceDescriptor[services.Count];
                services.CopyTo(items, 0);
                return items;
            }
        }
    }
}
