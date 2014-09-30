using System;
using System.Collections.Generic;

namespace Funke
{
    public interface IHasContainer
    {
        Container Container { get; }
    }

	public class Container : IDisposable
	{
	    public void AutoWire(object instance){ }

		public void RegisterAutoWiredType(Type serviceType, Type inFunqAsType, ReuseScope scope) { }
        public void RegisterAutoWiredTypes(IEnumerable<Type> serviceTypes, ReuseScope scope) { }
        public void RegisterAutoWired<T>() { }
        public void RegisterAutoWired<T, TAs>() { }
        public void RegisterAutoWiredAs<T, TAs>() { }
	    public void RegisterAutoWiredType(Type type) { }
	    public void RegisterAutoWiredTypes(HashSet<Type> types) { }

	    public void Register<T>(T instance) { }
	    public void Register(Func<Container, object> f) { }

	    public T TryResolve<T>() { throw new NotImplementedException(); }
	    public T Resolve<T>() { throw new NotImplementedException(); }

	    public Owner DefaultOwner { get; set; }

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

	public interface IContainerModule : IFunkelet
	{
	}
}