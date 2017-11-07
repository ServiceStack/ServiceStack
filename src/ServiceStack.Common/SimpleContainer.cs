using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Configuration;

namespace ServiceStack
{
    public class SimpleContainer : IContainer, IResolver 
    {
        public HashSet<string> IgnoreTypesNamed { get; } = new HashSet<string>();

        protected readonly ConcurrentDictionary<Type, object> InstanceCache = new ConcurrentDictionary<Type, object>();

        protected readonly ConcurrentDictionary<Type, Func<object>> Factory = new ConcurrentDictionary<Type, Func<object>>();

        public object Resolve(Type type)
        {
            Factory.TryGetValue(type, out Func<object> fn);
            return fn?.Invoke();
        }

        public bool Exists(Type type) => Factory.ContainsKey(type);

        public object RequiredResolve(Type type, Type ownerType)
        {
            var instance = Resolve(type);
            if (instance == null)
                throw new Exception($"Required Type of '{type.Name}' in '{ownerType.Name}' constructor was not registered in '{GetType().Name}'");

            return instance;
        }

        public IContainer AddSingleton(Type type, Func<object> factory)
        {
            Factory[type] = () => InstanceCache.GetOrAdd(type, factory());
            return this;
        }

        public IContainer AddTransient(Type type, Func<object> factory)
        {
            Factory[type] = factory;
            return this;
        }

        public T TryResolve<T>() => (T) Resolve(typeof(T));

        protected virtual bool IncludeProperty(PropertyInfo pi)
        {
            return pi.CanWrite
                   && !pi.PropertyType.IsValueType
                   && pi.PropertyType != typeof(string)
                   && pi.PropertyType != typeof(object)
                   && !IgnoreTypesNamed.Contains(pi.PropertyType.FullName);
        }

        protected virtual ConstructorInfo ResolveBestConstructor(Type type)
        {
            return type.GetConstructors()
                .OrderByDescending(x => x.GetParameters().Length) //choose constructor with most params
                .FirstOrDefault(ctor => !ctor.IsStatic);
        }

        public Func<object> CreateFactory(Type type)
        {
            var containerParam = Expression.Constant(this);
            var memberBindings = type.GetPublicProperties()
                .Where(IncludeProperty)
                .Select(x =>
                    Expression.Bind
                    (
                        x,
                        Expression.TypeAs(Expression.Call(containerParam, GetType().GetMethod(nameof(Resolve)), Expression.Constant(x.PropertyType)), x.PropertyType)
                    )
                ).ToArray();

            var ctorWithMostParameters = ResolveBestConstructor(type);
            if (ctorWithMostParameters == null)
                throw new Exception($"Constructor not found for Type '{type.Name}");

            var constructorParameterInfos = ctorWithMostParameters.GetParameters();
            var regParams = constructorParameterInfos
                .Select(x => 
                    Expression.TypeAs(Expression.Call(containerParam, GetType().GetMethod(nameof(RequiredResolve)), Expression.Constant(x.ParameterType), Expression.Constant(type)), x.ParameterType)
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
        
        public void Dispose()
        {
            var hold = InstanceCache;
            InstanceCache.Clear();
            foreach (var instance in hold)
            {
                try
                {
                    using (instance.Value as IDisposable) {}
                }
                catch { /* ignored */ }
            }
        }
    }

    public static class ContainerExtensions
    {
        public static T Resolve<T>(this IContainer container) =>
            (T)container.Resolve(typeof(T));

        public static bool Exists<T>(this IContainer container) => container.Exists(typeof(T));

        public static IContainer AddTransient<TService>(this IContainer container) => 
            container.AddTransient(typeof(TService), container.CreateFactory(typeof(TService)));

        public static IContainer AddTransient<TService>(this IContainer container, Func<TService> factory) => 
            container.AddTransient(typeof(TService), () => factory());

        public static IContainer AddTransient<TService, TImpl>(this IContainer container) where TImpl : TService => 
            container.AddTransient(typeof(TService), container.CreateFactory(typeof(TImpl)));

        public static IContainer AddTransient(this IContainer container, Type type) => 
            container.AddTransient(type, container.CreateFactory(type));

        public static IContainer AddSingleton<TService>(this IContainer container) => 
            container.AddSingleton(typeof(TService), container.CreateFactory(typeof(TService)));

        public static IContainer AddSingleton<TService>(this IContainer container, Func<TService> factory) => 
            container.AddSingleton(typeof(TService), () => factory());

        public static IContainer AddSingleton<TService, TImpl>(this IContainer container) where TImpl : TService => 
            container.AddSingleton(typeof(TService), container.CreateFactory(typeof(TImpl)));

        public static IContainer AddSingleton(this IContainer container, Type type) => 
            container.AddSingleton(type, container.CreateFactory(type));
    }
}