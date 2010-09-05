using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
	public class ExpressionTypeFunqContainer
		: ITypeFactory
	{
		protected Container container;

		public ReuseScope Scope { get; set; }

		private readonly Dictionary<Type, Func<object>> resolveFnMap = new Dictionary<Type, Func<object>>();

		public ExpressionTypeFunqContainer(Container container)
		{
			this.container = container;
			this.Scope = ReuseScope.None;
		}

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


		public Func<Container, TService> AutoWire<TService>(ParameterExpression lambdaParam)
		{
			var serviceType = typeof(TService);

			var propertyResolveFn = typeof(Container).GetMethod("TryResolve", new Type[0]);
			var memberBindings = serviceType.GetPublicProperties()
				.Where(x => x.CanWrite)
				.Select(x =>
					Expression.Bind
					(
						x,
						ResolveTypeExpression(propertyResolveFn, x.PropertyType, lambdaParam)
					)
				).ToArray();

			var ctorResolveFn = typeof(Container).GetMethod("Resolve", new Type[0]);
			return Expression.Lambda<Func<Container, TService>>
				(
					Expression.MemberInit
					(
						ConstrcutorExpression(ctorResolveFn, serviceType, lambdaParam),
						memberBindings
					),
					lambdaParam
				).Compile();
		}

		private static NewExpression ConstrcutorExpression(
			MethodInfo resolveMethodInfo, Type type, Expression lambdaParam)
		{
			var ctorWithMostParameters = GetConstructorWithMostParams(type);

			var constructorParameterInfos = ctorWithMostParameters.GetParameters();
			var regParams = constructorParameterInfos
				.Select(pi => ResolveTypeExpression(resolveMethodInfo, pi.ParameterType, lambdaParam));

			return Expression.New(ctorWithMostParameters, regParams.ToArray());
		}

		private static MethodCallExpression ResolveTypeExpression(
			MethodInfo resolveFn, Type resolveType, Expression lambdaParam)
		{
			var method = resolveFn.MakeGenericMethod(resolveType);
			return Expression.Call(lambdaParam, method);
		}

		public void Register<T>()
		{
			var lambdaParam = Expression.Parameter(typeof(Container), "lambdaContainerParam");

			var serviceFactory = AutoWire<T>(lambdaParam);

			this.container.Register(serviceFactory)
				.ReusedWithin(this.Scope);
		}

		public void RegisterTypes(params Type[] serviceTypes)
		{
			RegisterTypes((IEnumerable<Type>) serviceTypes);
		}

		public void RegisterTypes(IEnumerable<Type> serviceTypes)
		{
			foreach (var serviceType in serviceTypes)
			{
				var methodInfo = GetType().GetMethod("Register", new Type[0]);
				var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType });
				registerMethodInfo.Invoke(this, new object[0]);

				GenerateServiceFactory(serviceType);
			}
		}

		private void GenerateServiceFactory(Type type)
		{
			var containerInstance = Expression.Constant(this.container);
			var resolveInstance = Expression.Call(containerInstance, "Resolve", new[] { type }, new Expression[0]);
			var resolveObject = Expression.Convert(resolveInstance, typeof(object));
			var resolveFn = Expression.Lambda<Func<object>>(resolveObject, new ParameterExpression[0]).Compile();
			this.resolveFnMap[type] = resolveFn;
		}

		public object CreateInstance(Type type)
		{
			Func<object> resolveFn;

			if (!this.resolveFnMap.TryGetValue(type, out resolveFn))
			{
				throw new ResolutionException(type);
			}

			return resolveFn();
		}
	}
}