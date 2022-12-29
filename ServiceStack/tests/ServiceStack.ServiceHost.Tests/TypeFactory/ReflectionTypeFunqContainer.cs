using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
    /// <summary>
    /// Reflection example provided for performance comparisons
    /// </summary>
    public class ReflectionTypeFunqContainer
        : ITypeFactory
    {
        protected Container container;
        public ReuseScope Scope { get; set; }

        public ReflectionTypeFunqContainer(Container container)
        {
            this.container = container;
            this.Scope = ReuseScope.None;
        }

        protected static MethodInfo GetResolveMethod(Type typeWithResolveMethod, Type serviceType)
        {
            var methodInfo = typeWithResolveMethod.GetMethod("Resolve", new Type[0]);
            return methodInfo.MakeGenericMethod(new[] { serviceType });
        }

        public static ConstructorInfo GetConstructorWithMostParams(Type type)
        {
            return type.GetConstructors()
                .OrderByDescending(x => x.GetParameters().Length)
                .First(ctor => !ctor.IsStatic);
        }

        public Func<TService> AutoWire<TService>(Func<Type, object> resolveFn)
        {
            var serviceType = typeof(TService);
            var ci = GetConstructorWithMostParams(serviceType);

            var paramValues = new List<object>();
            var ciParams = ci.GetParameters();
            foreach (var parameterInfo in ciParams)
            {
                var paramValue = resolveFn(parameterInfo.ParameterType);
                paramValues.Add(paramValue);
            }

            var service = ci.Invoke(paramValues.ToArray());

            foreach (var propertyInfo in serviceType.GetProperties())
            {
                if (propertyInfo.PropertyType.IsValueType) continue;

                var propertyValue = resolveFn(propertyInfo.PropertyType);
                var propertySetter = propertyInfo.GetSetMethod();
                if (propertySetter != null)
                {
                    propertySetter.Invoke(service, new[] { propertyValue });
                }
            }

            return () => (TService)service;
        }

        private static Func<Type, object> Resolve(Container container)
        {
            return delegate (Type serviceType)
            {
                var resolveMethodInfo = GetResolveMethod(container.GetType(), serviceType);
                return resolveMethodInfo.Invoke(container, new object[0]);
            };
        }

        public void Register<T>()
        {
            //Everything from here needs to be optimized
            Func<Container, T> registerFn = delegate (Container c)
            {
                Func<T> serviceFactoryFn = AutoWire<T>(Resolve(c));
                return serviceFactoryFn();
            };

            this.container.Register(registerFn).ReusedWithin(this.Scope);
        }

        public void Register(params Type[] serviceTypes)
        {
            RegisterTypes(serviceTypes);
        }

        public void RegisterTypes(IEnumerable<Type> serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                var methodInfo = GetType().GetMethod("Register", new Type[0]);
                var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType });
                registerMethodInfo.Invoke(this, new object[0]);
            }
        }

        public object CreateInstance(IResolver resolver, Type type)
        {
            var factoryFn = Resolve(this.container);
            return factoryFn(type);
        }
    }
}