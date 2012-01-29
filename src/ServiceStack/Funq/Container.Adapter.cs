using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using System;

namespace Funq
{
	public partial class Container
	{
		public IContainerAdapter Adapter { get; set; }

		private AutoWireContainer autoWireContainer;
		internal AutoWireContainer AutoWireContainer
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

		/// <summary>
		/// Registers the types in the IoC container and
		/// adds auto-wiring to the specified types.
		/// Additionaly the creation of the types is cached when calling <see cref="CreateInstance"/> on the same instance.
		/// </summary>
		/// <param name="serviceTypes"></param>
		public void RegisterTypes(params Type[] serviceTypes)
		{
			AutoWireContainer.RegisterTypes(serviceTypes);
		}

		/// <summary>
		/// Registers the type in the IoC container and
		/// adds auto-wiring to the specified type.
		/// Additionaly the creation of the type is cached when calling <see cref="CreateInstance"/> on the same instance.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="inFunqAsType"></param>
		public void RegisterType(Type serviceType, Type inFunqAsType)
		{
			AutoWireContainer.RegisterType(serviceType, inFunqAsType);
		}

	}

}