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

        // This cascade of Lazy<> lamda constructors implements these requirements.

        private static readonly Lazy<ContainerBuilder> ContainerBuilder = new Lazy<ContainerBuilder>();
        private static readonly Lazy<IContainer> Container = new Lazy<IContainer>(() => ContainerBuilder.Value.Build());
        private static readonly Lazy<ILifetimeScope> RootLifetimeScope =
            new Lazy<ILifetimeScope>(() => Container.Value.BeginLifetimeScope());

        public void RegisterType(Type type, Sharing sharing = Sharing.None)
        {
            var registration = ContainerBuilder.Value.RegisterType(type);
            SetSharing(registration, sharing);
        }

        public ContainerBuilder GetContainerBuilderObsolete()
        {
            return ContainerBuilder.Value;
        }

        public void RegisterTypeAsInterface(
            Type classType,
            Type interfaceType,
            Sharing sharing = Sharing.None)
        {
            var registration = ContainerBuilder.Value.RegisterType(classType).As(interfaceType);
            SetSharing(registration, sharing);
        }

        private void SetSharing(
            IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration,
            Sharing sharing)
        {
            switch (sharing)
            {
                case Sharing.None:
                    registration.InstancePerDependency();
                    break;
                case Sharing.PerRequest:
                    //registration.InstancePerRequest();
                    throw new ApplicationException("Per Request DI not currentnly supported in MSA");
                    break;
                case Sharing.Singleton:
                    registration.RegistrationData.Sharing = InstanceSharing.Shared;
                    registration.RegistrationData.Lifetime = new RootScopeLifetime();
                    break;
            }
        }

        public DependencyResolver CreateResolver()
        {
            return new DependencyResolver(RootLifetimeScope.Value.BeginLifetimeScope());
        }

        public T TryResolve<T>()
        {
            try
            {
                return RootLifetimeScope.Value.Resolve<T>();
            }
            catch (DependencyResolutionException unusedException)
            {
                return default(T);
            }
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
