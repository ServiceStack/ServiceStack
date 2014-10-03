using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;

namespace Funke
{
    public class DependencyInjector
    {
        public IContainer Container { get; set;  }
        public ContainerBuilder Builder { get; set;  }
        public Funke.Container FunkeContainer { get; set;  }

        public DependencyInjector()
        {
            Builder = new ContainerBuilder();
            Container = Builder.Build();
            FunkeContainer = new Funke.Container();
        }

	    public void AutoWire(object instance){ } // ServiceRunner, EndPointHost
	    public void RegisterAutoWiredType(Type type) { } // ServiceManager
	    public void RegisterAutoWiredTypes(HashSet<Type> types) { } // ServiceManager
        public void RegisterAutoWiredAs<T, TAs>() { } // AppHostBase, HttpListenerBase
        public void RegisterAutoWired<T>() { } // ServiceManager

	    public void Register<T>(T instance) { } // AppHostBase, HttpListenerBase, EndpointHost

	    public T Resolve<T>() { throw new NotImplementedException(); } // AppHostBase, HttpListenerBase
	    public T TryResolve<T>() { throw new NotImplementedException(); } // HttpListenerRequestWrapper, HttpRequestWrapper, AppHostBase, HttpListenerBase, ValidationFeature [ServiceInterface]

	    public void Register(Func<Container, object> f) { } // ValidationFeature [ServiceStack.erviceInterface], EndpointHost
		public void RegisterAutoWiredType(Type serviceType, Type inFunqAsType, ReuseScope scope) { } // ValidationFeature [ServiceInterface]

	    public Owner DefaultOwner { get; set; } // Not really used. can be tracked down and deleted.

	    public void Dispose()
	    {
	    }
	}

    public interface IHasDependencyInjector
    {
        DependencyInjector DependencyInjector { get; }
    }

    public interface IHasContainer
    {
        Container Container { get; }
    }

	public class Container : IDisposable
	{
	    private readonly static ContainerBuilder _builder = new ContainerBuilder();

	    public void AutoWire(object instance){ } // ServiceRunner, EndPointHost
	    public void RegisterAutoWiredType(Type type) { } // ServiceManager
	    public void RegisterAutoWiredTypes(HashSet<Type> types) { } // ServiceManager
        public void RegisterAutoWiredAs<T, TAs>() { } // AppHostBase, HttpListenerBase
        public void RegisterAutoWired<T>() { } // ServiceManager

	    public void Register<T>(T instance) { } // AppHostBase, HttpListenerBase, EndpointHost

	    public T Resolve<T>() { throw new NotImplementedException(); } // AppHostBase, HttpListenerBase
	    public T TryResolve<T>() { throw new NotImplementedException(); } // HttpListenerRequestWrapper, HttpRequestWrapper, AppHostBase, HttpListenerBase, ValidationFeature [ServiceInterface]

	    public void Register(Func<Container, object> f) { } // ValidationFeature [ServiceStack.erviceInterface], EndpointHost
		public void RegisterAutoWiredType(Type serviceType, Type inFunqAsType, ReuseScope scope) { } // ValidationFeature [ServiceInterface]

	    public Owner DefaultOwner { get; set; } // Not really used. can be tracked down and deleted.

	    public void Dispose()
	    {
	    }
	}

	public enum Owner
	{
		Container,
		External,
		Default,
	}

	public interface IFunkelet
	{
		void Configure(Container container);
	}

	public enum ReuseScope
	{
		Hierarchy, 
		Container, 
		None,
        Request,
		Default = Hierarchy,
	}
}