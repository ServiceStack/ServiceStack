using System;
using System.Collections.Generic;
using Funq;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
	public class ReflectiveFunqlet
		: FunqletBase
	{
		public ReflectiveFunqlet(IEnumerable<Type> serviceTypes) : base(serviceTypes) {}

		public ReflectiveFunqlet(params Type[] serviceTypes) : base(serviceTypes) {}

		public Func<TService> AutoWire<TService>(Func<Type, object> resolveFn)
		{
			var serviceType = typeof(TService);
			var ci = GetConstructorWithMostParams(serviceType);

			var paramValues = new List<object>();
			var ciParams = ci.GetParameters();
			foreach (var parameterInfo in ciParams)
			{
				var paramValue = resolveFn(parameterInfo.ParameterType);
				paramValues.Add(paramValue);
			}

			var service = ci.Invoke(paramValues.ToArray());

			foreach (var propertyInfo in serviceType.GetProperties())
			{
				var propertyValue = resolveFn(propertyInfo.PropertyType);
				var propertySetter = propertyInfo.GetSetMethod();
				if (propertySetter != null)
				{
					propertySetter.Invoke(service, new[] { propertyValue });
				}
			}

			return () => (TService)service;
		}

		private static Func<Type, object> Resolve(Container container)
		{
			return delegate(Type serviceType) {
				var resolveMethodInfo = GetResolveMethod(container.GetType(), serviceType);
				return resolveMethodInfo.Invoke(container, new object[0]);
			};
		}

		public void Register<T>()
		{
			//Everything from here needs to be optimized
			Func<Container, T> registerFn = delegate(Container container) {
				Func<T> serviceFactoryFn = AutoWire<T>(Resolve(container));
				return serviceFactoryFn();
			};

			this.Container.Register(registerFn).ReusedWithin(this.Scope);
		}

		protected override void Run()
		{
			foreach (var serviceType in serviceTypes)
			{
				var methodInfo = GetType().GetMethod("Register", new Type[0]);
				var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType });
				registerMethodInfo.Invoke(this, new object[0]);
			}
		}
	}
}