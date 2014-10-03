using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using DependencyInjection;
using DependencyInjection.Funq;

namespace DependencyInjection
{
    public class DependencyInjector : IDisposable
    {
        private static IContainer _container;
        private static ContainerBuilder _builder;
        public IContainer Container { // DAC make this unmockable
            get
            {
                _container = _container ?? Builder.Build();
                return _container;
            } 
        }
        public ContainerBuilder Builder {
            get { return _builder; }
        }
        public Funkee.Container FunkeContainer { get; set; }

        public DependencyInjector()
        {

            _builder = _builder ?? new ContainerBuilder();
            //Container = null;
            FunkeContainer = new Funkee.Container();
        }

        public void AutoWire(object instance) // ServiceRunner, EndPointHost
            { throw new NotImplementedException("AutoWire(object)"); }
        public void RegisterAutoWiredType(Type type) // ServiceManager
            { throw new NotImplementedException("AutoWiredType(Type)"); }
        public void RegisterAutoWiredTypes(HashSet<Type> types)
        {
            foreach (var t in types)
            {
                Builder.RegisterType(t);
            }
        }
        public void RegisterAutoWiredAs<T, TAs>() // AppHostBase, HttpListenerBase
            { throw new NotImplementedException("RegisterAutoWiredAs<T, TAs>()"); }
        public void RegisterAutoWired<T>() // ServiceManager
            { throw new NotImplementedException("RegisterAutoWired<T>()"); }

        public void Register<T>(T instance) // AppHostBase, HttpListenerBase, EndpointHost
            { throw new NotImplementedException("Register<T>(instance)"); }

        public T Resolve<T>() // AppHostBase, HttpListenerBase
        {
            return Container.Resolve<T>();
        }

        public T TryResolve<T>() // AppHostBase, HttpListenerBase
        {
            try
            {
                return Container.Resolve<T>();
            }
            catch
            {
                return default(T); // DAC 'return null' did not work. Should I require "T : class" all up above?
            }
        }

        public object Resolve(Type type) // AppHostBase, HttpListenerBase
        {
            return Container.Resolve(type);
        }

        public void Register(Func<Container, object> f)// ValidationFeature [ServiceStack.erviceInterface], EndpointHost
            { throw new NotImplementedException(""); }
        public void RegisterAutoWiredType(Type serviceType, Type inFunqAsType, Funq.ReuseScope scope) // ValidationFeature [ServiceInterface]
            { throw new NotImplementedException(""); }

        public void Dispose()
        {
            FunkeContainer.Dispose();
        }
    }

    public interface IHasDependencyInjector
    {
        DependencyInjector DependencyInjector { get; }
    }

	public interface IConfigureDependencyInjector
	{
		void Configure(DependencyInjector dependencyInjector);
	}

    namespace Funq
    {
        public enum ReuseScope
        {
            Hierarchy,
            Container,
            None,
            Request,
            Default = Hierarchy,
        }
    }
}

namespace Funkee
{
	public class Container : IDisposable
	{
	    private readonly static ContainerBuilder _builder = new ContainerBuilder();

	    public void AutoWire(object instance){ } // ServiceRunner, EndPointHost
	    public void RegisterAutoWiredType(Type type) { } // ServiceManager
	    public void RegisterAutoWiredTypes(HashSet<Type> types) { } // ServiceManager
        public void RegisterAutoWiredAs<T, TAs>() { } // AppHostBase, HttpListenerBase
        public void RegisterAutoWired<T>() { } // ServiceManager

	    public void Register<T>(T instance) { } // AppHostBase, HttpListenerBase, EndpointHost

	    public T Resolve<T>() { throw new NotImplementedException("DAC 0"); } // AppHostBase, HttpListenerBase
	    public T TryResolve<T>() { throw new NotImplementedException("DAC 1"); } // HttpListenerRequestWrapper, HttpRequestWrapper, AppHostBase, HttpListenerBase, ValidationFeature [ServiceInterface]

	    public void Register(Func<Container, object> f) { } // ValidationFeature [ServiceStack.erviceInterface], EndpointHost
		public void RegisterAutoWiredType(Type serviceType, Type inFunqAsType, ReuseScope scope) { } // ValidationFeature [ServiceInterface]

	    public void Dispose()
	    {
	    }
	}

}