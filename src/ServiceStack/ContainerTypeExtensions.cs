using System;
using System.Collections.Generic;
using System.Linq;
using Funq;

namespace ServiceStack
{
    public static class ContainerTypeExtensions
    {
        /// <summary>
        /// Registers the type in the IoC container and
        /// adds auto-wiring to the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="inFunqAsType"></param>
        public static void RegisterAutoWiredType(this Container container, Type serviceType, Type inFunqAsType,
            ReuseScope scope = ReuseScope.None)
        {
            if (serviceType.IsAbstract() || serviceType.ContainsGenericParameters())
                return;

            var methodInfo = typeof(Container).GetMethodInfo("RegisterAutoWiredAs", Type.EmptyTypes);
            var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType, inFunqAsType });

            var registration = registerMethodInfo.Invoke(container, TypeConstants.EmptyObjectArray) as IRegistration;
            registration.ReusedWithin(scope);
        }

        /// <summary>
        /// Registers a named instance of type in the IoC container and
        /// adds auto-wiring to the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="inFunqAsType"></param>
        public static void RegisterAutoWiredType(this Container container, string name, Type serviceType, Type inFunqAsType,
            ReuseScope scope = ReuseScope.None)
        {
            if (serviceType.IsAbstract() || serviceType.ContainsGenericParameters())
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
        /// <param name="serviceTypes"></param>
        public static void RegisterAutoWiredType(this Container container, Type serviceType,
            ReuseScope scope = ReuseScope.None)
        {
            //Don't try to register base service classes
            if (serviceType.IsAbstract() || serviceType.ContainsGenericParameters())
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
        /// <param name="serviceTypes"></param>
        public static void RegisterAutoWiredType(this Container container, string name, Type serviceType,
            ReuseScope scope = ReuseScope.None)
        {
            //Don't try to register base service classes
            if (serviceType.IsAbstract() || serviceType.ContainsGenericParameters())
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
        /// <param name="serviceTypes"></param>
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
                .GetMethodInfos()
                .First(x => x.Name == "Register" && x.GetParameters().Length == 1 && x.ReturnType == typeof(void))
                .MakeGenericMethod(asType);

            mi.Invoke(container, new[] { instance });
            return container;
        }
    }
}
