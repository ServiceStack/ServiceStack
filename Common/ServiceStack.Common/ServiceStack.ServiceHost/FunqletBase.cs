using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;

namespace ServiceStack.ServiceHost
{
	public abstract class FunqletBase
		: IFunqlet
	{
		protected IEnumerable<Type> ServiceTypes { get; set; }
		protected Container Container { get; set; }

		public ReuseScope Scope { get; set; }

		protected FunqletBase(IEnumerable<Type> serviceTypes)
		{
			this.ServiceTypes = serviceTypes;
			this.Scope = ReuseScope.None;
		}

		protected FunqletBase(params Type[] serviceTypes)
			: this((IEnumerable<Type>)serviceTypes) {}

		public void Configure(Container container)
		{
			this.Container = container;
			Run();
		}

		protected abstract void Run();

		protected static MethodInfo GetResolveMethod(Type typeWithResolveMethod, Type serviceType)
		{
			var methodInfo = typeWithResolveMethod.GetMethod("Resolve", new Type[0]);
			return methodInfo.MakeGenericMethod(new[] { serviceType });
		}

		public static ConstructorInfo GetConstructorWithMostParams(Type type)
		{
			return type.GetConstructors()
				.OrderByDescending(x => x.GetParameters().Length)
				.Where(ctor => !ctor.IsStatic)
				.First();
		}

		public void ConfigureExpression(Container container)
		{
			this.Container = container;

			foreach (var serviceType in ServiceTypes)
			{

				var methodInfo = GetType().GetMethod("RegisterExpression", new Type[0]);
				var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType });
				registerMethodInfo.Invoke(this, new object[0]);
			}
		}
	}
}