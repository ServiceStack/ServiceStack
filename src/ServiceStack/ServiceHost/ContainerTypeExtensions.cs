using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Funq;

namespace ServiceStack.ServiceHost
{
	public static class ContainerTypeExtensions
	{
		/// <summary>
		/// Registers the type in the IoC container and
		/// adds auto-wiring to the specified type.
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="inFunqAsType"></param>
		public static IRegistration RegisterAutoWiredType(this Container container, Type serviceType, Type inFunqAsType,
			ReuseScope scope = ReuseScope.None)
		{
			if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
				throw new ArgumentException("Can not register abstract/generic types!");

			var methodInfo = typeof(Container).GetMethod("RegisterAutoWiredAs", Type.EmptyTypes);
			var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType, inFunqAsType });

			var registration = registerMethodInfo.Invoke(container, new object[0]) as IRegistration;
			registration.ReusedWithin(scope);

			return registration;
		}

		/// <summary>
		/// Registers the type in the IoC container and
		/// adds auto-wiring to the specified type.
		/// The reuse scope is set to none (transient).
		/// </summary>
		/// <param name="serviceTypes"></param>
		public static void RegisterAutoWiredType(this Container container, Type serviceType)
		{
			//Don't try to register base service classes
			if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
				return;

			var methodInfo = typeof(Container).GetMethod("RegisterAutoWired", Type.EmptyTypes);
			var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType });

			var registration = registerMethodInfo.Invoke(container, new object[0]) as IRegistration;
			registration.ReusedWithin(ReuseScope.None);
		}

		/// <summary>
		/// Registers the types in the IoC container and
		/// adds auto-wiring to the specified types.
		/// The reuse scope is set to none (transient).
		/// </summary>
		/// <param name="serviceTypes"></param>
		public static void RegisterAutoWiredTypes(this Container container, IEnumerable<Type> serviceTypes)
		{
			foreach (var serviceType in serviceTypes)
			{
				//Don't try to register base service classes
				if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
					continue;

				var methodInfo = typeof(Container).GetMethod("RegisterAutoWired", Type.EmptyTypes);
				var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType });

				var registration = registerMethodInfo.Invoke(container, new object[0]) as IRegistration;
				registration.ReusedWithin(ReuseScope.None);
			}
		}
	}
}
