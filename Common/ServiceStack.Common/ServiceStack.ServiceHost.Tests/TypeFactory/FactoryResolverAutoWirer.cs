using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Funq;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
	public class FactoryResolverAutoWirer
	{
		public Func<T> AutoWire<T>(Func<Type, object> resolveFn)
		{
			var serviceType = typeof(T);
			var ci = GetConstructorWithMostParameters(serviceType);

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

			return () => (T)service;
		}

		private static ConstructorInfo GetConstructorWithMostParameters(Type type)
		{
			return type
				.GetConstructors()
				.OrderByDescending(x => x.GetParameters().Length)
				.Where(ctor => !ctor.IsStatic)
				.First();
		}
	}

	public class Funqlet
		: IFunqlet
	{
		private readonly IEnumerable<Type> serviceTypes;
		readonly FactoryResolverAutoWirer autoWirer = new FactoryResolverAutoWirer();
		private Container Container;

		public Funqlet(IEnumerable<Type> serviceTypes)
		{
			this.serviceTypes = serviceTypes;
		}

		public Funqlet(params Type[] serviceTypes)
		{
			this.serviceTypes = serviceTypes;
		}

		public void Register<T>()
		{
			//Everything from here needs to be optimized
			Func<Container, T> registerFn = delegate(Container container) {
				Func<T> serviceFactoryFn = autoWirer.AutoWire<T>(Resolve(container));
				return serviceFactoryFn();
			};

			this.Container.Register(registerFn);
		}

		private static Func<Type, object> Resolve(Container container)
		{
			return delegate(Type serviceType) {
				var resolveMethodInfo = GetResolveMethod(container.GetType(), serviceType);
				return resolveMethodInfo.Invoke(container, new object[0]);
			};
		}

		private static MethodInfo GetResolveMethod(Type typeWithResolveMethod, Type serviceType)
		{
			foreach (var methodInfo in typeWithResolveMethod.GetMethods())
			{
				if (methodInfo.Name == "Resolve"
					&& methodInfo.GetGenericArguments().Length == 1)
				{
					return methodInfo.MakeGenericMethod(new[] { serviceType });
				}
			}
			throw new NotImplementedException("Resolve");
		}

		private static MethodInfo GetRegisterMethod(Type typeWithRegisterMethod, Type serviceType)
		{
			var factoryTypeMethods = typeWithRegisterMethod.GetMethods();
			foreach (var methodInfo in factoryTypeMethods)
			{
				if (methodInfo.Name == "Register"
					&& methodInfo.GetGenericArguments().Length == 1)
				{
					return methodInfo.MakeGenericMethod(new[] { serviceType });
				}
			}
			throw new NotImplementedException("Register");
		}

		public void Configure(Container container)
		{
			this.Container = container;

			foreach (var serviceType in serviceTypes)
			{
				var registerMethodInfo = GetRegisterMethod(this.GetType(), serviceType);
				registerMethodInfo.Invoke(this, new object[0]);
			}
		}
	}


	/// <summary>
	/// Funq helper for easy registration.
	/// </summary>
	public static class FunqEasyRegistrationHelper
	{
		/// <summary>
		/// Register a service with the default, look-up-all_dependencies-from-the-container behavior.
		/// </summary>
		/// <typeparam name="interfaceT">interface type</typeparam>
		/// <typeparam name="implT">implementing type</typeparam>
		/// <param name="container">Funq container</param>
		public static void EasyRegister<interfaceT, implT>(this Container container) where implT : interfaceT
		{
			var lambdaParam = Expression.Parameter(typeof(Container), "ref_to_the_container_passed_into_the_lambda");

			var constructorExpression = BuildImplConstructorExpression<implT>(lambdaParam);
			var compiledExpression = CompileInterfaceConstructor<interfaceT>(lambdaParam, constructorExpression);

			container.Register(compiledExpression);
		}

		private static readonly MethodInfo FunqContainerResolveMethod;

		static FunqEasyRegistrationHelper()
		{
			FunqContainerResolveMethod = typeof(Container).GetMethod("Resolve", new Type[0]);
		}

		private static NewExpression BuildImplConstructorExpression<implT>(Expression lambdaParam)
		{
			var ctorWithMostParameters = GetConstructorWithMostParameters<implT>();

			var constructorParameterInfos = ctorWithMostParameters.GetParameters();
			var regParams = constructorParameterInfos.Select(pi => GetParameterCreationExpression(pi, lambdaParam));

			return Expression.New(ctorWithMostParameters, regParams.ToArray());
		}

		private static Func<Container, interfaceT> CompileInterfaceConstructor<interfaceT>(ParameterExpression lambdaParam, Expression constructorExpression)
		{
			var constructorLambda = Expression.Lambda<Func<Container, interfaceT>>(constructorExpression, lambdaParam);
			return constructorLambda.Compile();
		}

		private static ConstructorInfo GetConstructorWithMostParameters<implT>()
		{
			return typeof(implT)
				.GetConstructors()
				.OrderBy(x => x.GetParameters().Length)
				.Where(ctor => !ctor.IsStatic)
				.Last();
		}

		private static MethodCallExpression GetParameterCreationExpression(ParameterInfo pi, Expression lambdaParam)
		{
			var method = FunqContainerResolveMethod.MakeGenericMethod(pi.ParameterType);
			return Expression.Call(lambdaParam, method);
		}
	}
}