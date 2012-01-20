using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using System;

namespace Funq
{
	public partial class Container
	{
		public IContainerAdapter Adapter { get; set; }

		private AutoWireContainer autoWireContainer;
		private AutoWireContainer AutoWireContainer
		{
			get
			{
				if (autoWireContainer == null)
					autoWireContainer = new AutoWireContainer(this);
				return autoWireContainer;
			}
		}

		/// <summary>
		/// Register an autowired dependency
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void RegisterAutoWired<T>()
		{
			AutoWireContainer.Register<T>();
		}

		/// <summary>
		/// Register an autowired dependency as a separate type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void RegisterAutoWiredAs<T, TAs>()
			where T : TAs
		{
			AutoWireContainer.RegisterAs<T, TAs>();
		}

		/// <summary>
		/// Alias for RegisterAutoWiredAs
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void RegisterAs<T, TAs>()
			where T : TAs
		{
			AutoWireContainer.RegisterAs<T, TAs>();
		}

		/// <summary>
		/// Auto-wires an existing instance, 
		/// ie all public properties are tried to be resolved.
		/// </summary>
		/// <param name="instance"></param>
		public void AutoWire(object instance)
		{
			AutoWireContainer.AutoWire(instance);
		}
	}

}