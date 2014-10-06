using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;

namespace ServiceStack.DependencyInjection
{
    public class DependencyService
    {
        // DAC clean up this static/singleton mess
        private readonly static Lazy<ContainerBuilder> _containerBuilder = new Lazy<ContainerBuilder>();
        private static IContainer _container = null;
        private static ILifetimeScope _rootLifetimeScope = null;
        public static IContainer Container {
            get
            {
                _container = _container ?? _containerBuilder.Value.Build();
                return _container;
            } 
        }

        public static ILifetimeScope RootLifetimeScope
        {
            get
            {
                // DAC fixme; not thread safe.
                _rootLifetimeScope = _rootLifetimeScope ?? Container.BeginLifetimeScope();
                return _rootLifetimeScope;
            }
        }

        public ILifetimeScope GetRootLifetimeScope()
        {
            return RootLifetimeScope;
        }

        public ContainerBuilder ContainerBuilder 
        {
            get { return _containerBuilder.Value; }
        }

        public void RegisterAutoWiredTypes(HashSet<Type> types)
        {
            foreach (var t in types)
            {
                _containerBuilder.Value.RegisterType(t);
            }
        }

        public static object Resolve(Type type, ILifetimeScope lifetimeScope = null)
        {
            lifetimeScope = lifetimeScope ?? RootLifetimeScope;
            return lifetimeScope.Resolve(type);
        }

        public T TryResolve<T>(ILifetimeScope lifetimeScope = null)
        {
            lifetimeScope = lifetimeScope ?? RootLifetimeScope;
            try
            {
                return lifetimeScope.Resolve<T>();
            }
            catch (DependencyResolutionException unusedException)
            {
                return default(T);
            }
        }

        // Old funq calls. Called in code branches that are expected to be dead.
        public void AutoWire(object instance) // ServiceRunner, EndPointHost
            { throw new NotImplementedException("AutoWire(object)"); }
        public void RegisterAutoWiredType(Type type) // ServiceManager
            { throw new NotImplementedException("AutoWiredType(Type)"); }
        public void RegisterAutoWiredAs<T, TAs>() // AppHostBase, HttpListenerBase
            { throw new NotImplementedException("RegisterAutoWiredAs<T, TAs>()"); }
        public void RegisterAutoWired<T>() // ServiceManager
            { throw new NotImplementedException("RegisterAutoWired<T>()"); }
        public void Register<T>(T instance) // AppHostBase, HttpListenerBase, EndpointHost
            { throw new NotImplementedException("Register<T>(instance)"); }
        public T Resolve<T>(ILifetimeScope lifetimeScope = null) // AppHostBase, HttpListenerBase
            { throw new NotImplementedException("Resolve<T>(ILifetimeScope)"); }
        public void Register(Func<Container, object> f)// ValidationFeature [ServiceStack.erviceInterface], EndpointHost
            { throw new NotImplementedException("Register(Func<Container, object>)"); }
        public void RegisterAutoWiredType(Type serviceType, Type inFunqAsType) // ValidationFeature [ServiceInterface]
            { throw new NotImplementedException(""); }

    }

    public interface IHasDependencyService
    {
        DependencyService DependencyService { get; }
    }

	public interface IConfigureDependencyService
	{
		void Configure(DependencyService dependencyService);
	}
}
