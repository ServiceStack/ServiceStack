using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using System;

namespace Funq
{
	public partial class Container
	{
		public IContainerAdapter Adapter { get; set; }

		/// <summary>
		/// Register an autowired dependency
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public IRegistration<T> RegisterAutoWired<T>()
		{
			var serviceFactory = AutoWireHelpers.GenerateAutoWireFn<T>();
			return this.Register(serviceFactory);
		}

		/// <summary>
		/// Register an autowired dependency as a separate type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public IRegistration<TAs> RegisterAutoWiredAs<T, TAs>()
			where T : TAs
		{
			var serviceFactory = AutoWireHelpers.GenerateAutoWireFn<T>();
			Func<Container, TAs> fn = c => serviceFactory(c);
			return this.Register(fn);
		}

		/// <summary>
		/// Alias for RegisterAutoWiredAs
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public IRegistration<TAs> RegisterAs<T, TAs>()
			where T : TAs
		{
			return this.RegisterAutoWiredAs<T, TAs>();
		}

		/// <summary>
		/// Auto-wires an existing instance, 
		/// ie all public properties are tried to be resolved.
		/// </summary>
		/// <param name="instance"></param>
		public void AutoWire(object instance)
		{
			AutoWireHelpers.AutoWire(this, instance);
		}
	}

}