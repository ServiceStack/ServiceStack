using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Lifetime;

namespace ServiceStack.DependencyInjection
{
    // Any of several 
    public interface IHasDependencyService
    {
        DependencyService DependencyService { get; }
    }

	public interface IConfigureDependencyService
	{
		void Configure(DependencyService dependencyService);
	}

    public class DependencyService
    {
        public enum Sharing { Singleton, PerRequest, None };

        // Autofac has three singleton objects, whose construction is a little tricky.
        // Specifically, the Container can't be constructed until all calls to "RegisterType()" have completed.
        // To make matters worse, the initial requests for these objects may occur in any order
        // (a.k.a. "The Principal of Maximum Surprise").

        private readonly object _lockObject;
        private ContainerBuilder _containerBuilder;
        private IContainer _container;

        public DependencyService()
        {
            _lockObject = new object();
            _containerBuilder = new ContainerBuilder();
        }

        public void RegisterTypeAsItself(Type classType, Sharing sharing = Sharing.None)
        {
            var registration = _containerBuilder.RegisterType(classType);
            SetSharing(registration, sharing);
        }

        public void RegisterTypeAsInterface(Type classType, Type interfaceType, Sharing sharing = Sharing.None)
        {
            var registration = _containerBuilder.RegisterType(classType).As(interfaceType);
            SetSharing(registration, sharing);
        }

        public void RegisterSingletonInstance(object classInstance, Type classOrInterfaceType)
        {
            var registration = _containerBuilder.Register(c => classInstance).As(classOrInterfaceType);
            SetSharing(registration, Sharing.Singleton);
        }

        public ContainerBuilder GetContainerBuilder()
        {
            return _containerBuilder;
        }

        public void UpdateRegistrations()
        {
            lock (_lockObject)
            {
                if (_container != null)
                {
                    _containerBuilder.Update(_container);
                }
                else
                {
                    _container = _containerBuilder.Build();
                }
                _containerBuilder = new ContainerBuilder();
            }
        }

        private void SetSharing(IRegistrationBuilder<object, object, SingleRegistrationStyle> registration,
                                Sharing sharing)
        {
            switch (sharing)
            {
                case Sharing.None:
                    registration.InstancePerDependency();
                    break;
                case Sharing.PerRequest:
                    registration.InstancePerLifetimeScope();
                    break;
                case Sharing.Singleton:
                    registration.SingleInstance();
                    break;
            }
        }

        public DependencyResolver CreateResolver()
        {
            if (_container == null)
            {
                UpdateRegistrations();
            }
            return new DependencyResolver(_container.BeginLifetimeScope());
        }

        public T TryResolve<T>()
        {
            return CreateResolver().TryResolve<T>();
        }

        // These methods are obsolete. They are called in code branches that are believed to be dead.
        // If any of that code ever becomes active, we need to know that, so the code 'throws' here.
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
}
