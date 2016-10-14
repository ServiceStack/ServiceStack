using System;
using Funq;

namespace Funq
{
    public partial class Container : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return this.TryResolve(serviceType);
        }
    }
}

namespace ServiceStack
{
    /// <summary>
    /// Wrappers for improving consistency with .NET Core Conventions
    /// </summary>
    public static class ContainerNetCoreExtensions
    {
        // Request Scoped dependencies

        public static Container AddScoped(this Container services, Type serviceType)
        {
            services.RegisterAutoWiredType(serviceType, ReuseScope.Request);
            return services;
        }

        public static Container AddScoped(this Container services, Type serviceType, Type implementationType)
        {
            services.RegisterAutoWiredType(implementationType, serviceType, ReuseScope.Request);
            return services;
        }

        public static Container AddScoped<TService>(this Container services) where TService : class
        {
            services.RegisterAutoWired<TService>().ReusedWithin(ReuseScope.Request);
            return services;
        }

        public static Container AddScoped<TService>(this Container services, Func<Container, TService> implementationFactory) where TService : class
        {
            services.Register(implementationFactory).ReusedWithin(ReuseScope.Request);
            return services;
        }

        public static Container AddScoped<TService, TImplementation>(this Container services)
            where TService : class
            where TImplementation : class, TService
        {
            services.RegisterAutoWiredAs<TImplementation, TService>().ReusedWithin(ReuseScope.Request);
            return services;
        }

        public static Container AddScoped<TService, TImplementation>(this Container services, Func<Container, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            services.Register<TService>(implementationFactory).ReusedWithin(ReuseScope.Request);
            return services;
        }

        // Singleton

        public static Container AddSingleton(this Container services, Type serviceType)
        {
            services.RegisterAutoWiredType(serviceType, ReuseScope.Container);
            return services;
        }

        public static Container AddSingleton(this Container services, Type serviceType, Type implementationType)
        {
            services.RegisterAutoWiredType(implementationType, serviceType, ReuseScope.Container);
            return services;
        }

        public static Container AddSingleton<TService>(this Container services) where TService : class
        {
            services.RegisterAutoWired<TService>().ReusedWithin(ReuseScope.Container);
            return services;
        }

        public static Container AddSingleton<TService>(this Container services, Func<Container, TService> implementationFactory) where TService : class
        {
            services.Register(implementationFactory).ReusedWithin(ReuseScope.Container);
            return services;
        }

        public static Container AddSingleton<TService, TImplementation>(this Container services)
            where TService : class
            where TImplementation : class, TService
        {
            services.RegisterAutoWiredAs<TImplementation, TService>().ReusedWithin(ReuseScope.Container);
            return services;
        }

        public static Container AddSingleton<TService>(this Container services, TService implementationInstance) where TService : class
        {
            services.Register(implementationInstance);
            return services;
        }

        public static Container AddSingleton<TService, TImplementation>(this Container services, Func<Container, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            services.Register<TService>(implementationFactory).ReusedWithin(ReuseScope.Container);
            return services;
        }

        // Transient / No Reuse

        public static Container AddTransient(this Container services, Type serviceType)
        {
            services.RegisterAutoWiredType(serviceType, ReuseScope.None);
            return services;
        }

        public static Container AddTransient(this Container services, Type serviceType, Type implementationType)
        {
            services.RegisterAutoWiredType(implementationType, serviceType, ReuseScope.None);
            return services;
        }

        public static Container AddTransient<TService>(this Container services) where TService : class
        {
            services.RegisterAutoWired<TService>().ReusedWithin(ReuseScope.None);
            return services;
        }

        public static Container AddTransient<TService>(this Container services, Func<Container, TService> implementationFactory) where TService : class
        {
            services.Register(implementationFactory).ReusedWithin(ReuseScope.None);
            return services;
        }

        public static Container AddTransient<TService, TImplementation>(this Container services)
            where TService : class
            where TImplementation : class, TService
        {
            services.RegisterAutoWiredAs<TImplementation, TService>().ReusedWithin(ReuseScope.None);
            return services;
        }

        public static Container AddTransient<TService, TImplementation>(this Container services, Func<Container, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            services.Register<TService>(implementationFactory).ReusedWithin(ReuseScope.None);
            return services;
        }
    }
}