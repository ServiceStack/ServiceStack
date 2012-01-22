using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.CacheAccess;
using System.Threading;
using System.Collections;

namespace ServiceStack.ServiceHost
{
	public class AutoWireContainer
		: ITypeFactory
	{
		protected Container container;

		private readonly Dictionary<Type, Func<object>> resolveFnMap = new Dictionary<Type, Func<object>>();
		private Dictionary<Type, Action<object>[]> autoWireCache = new Dictionary<Type, Action<object>[]>();

		/// <summary>
		/// Determines in which scope the types registered will be saved in the Funq container.
		/// </summary>
		public ReuseScope Scope { get; set; }

		public AutoWireContainer(Container container)
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
				.First(ctor => !ctor.IsStatic);
		}

		/// <summary>
		/// Generates a function which creates and auto-wires <see cref="TService"/>.
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <param name="lambdaParam"></param>
		/// <returns></returns>
		public Func<Container, TService> GenerateAutoWireFn<TService>()
		{
			var lambdaParam = Expression.Parameter(typeof(Container), "container");
			var propertyResolveFn = typeof(Container).GetMethod("TryResolve", new Type[0]);
			var memberBindings = typeof(TService).GetPublicProperties()
				.Where(x => x.CanWrite && !x.PropertyType.IsValueType)
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
						ConstrcutorExpression(ctorResolveFn, typeof(TService), lambdaParam),
						memberBindings
					),
					lambdaParam
				).Compile();
		}

		/// <summary>
		/// Auto-wires an existing instance of a specific type.
		/// The auto-wiring progress is also cached to be faster 
		/// when calling next time with the same type.
		/// </summary>
		/// <param name="instance"></param>
		public void AutoWire(object instance)
		{
			var instanceType = instance.GetType();
			var propertyResolveFn = typeof(Container).GetMethod("TryResolve", new Type[0]);

			Action<object>[] setters;
			if (!this.autoWireCache.TryGetValue(instanceType, out setters))
			{
				setters = instanceType.GetPublicProperties()
					.Where(x => x.CanWrite && !x.PropertyType.IsValueType)
					.Select(x => this.GenerateAutoWireFnForProperty(propertyResolveFn, x, instanceType))
					.ToArray();

				//Support for multiple threads is needed
				Dictionary<Type, Action<object>[]> snapshot, newCache;
				do
				{
					snapshot = autoWireCache;
					newCache = new Dictionary<Type, Action<object>[]>(autoWireCache);
					newCache[instanceType] = setters;
				} while (!ReferenceEquals(
				Interlocked.CompareExchange(ref autoWireCache, newCache, snapshot), snapshot));
			}

			foreach (var setter in setters)
				setter(instance);
		}

		private Action<object> GenerateAutoWireFnForProperty(
			MethodInfo propertyResolveFn, PropertyInfo property, Type instanceType)
		{
			var instanceParam = Expression.Parameter(typeof(object), "instance");
			var containerParam = Expression.Constant(this.container);

			Func<object, object> getter = Expression.Lambda<Func<object, object>>(
						Expression.Call(
							Expression.Convert(instanceParam, instanceType),
							property.GetGetMethod()
						),
						instanceParam
					).Compile();

			Action<object> setter = Expression.Lambda<Action<object>>(
				Expression.Call(
					Expression.Convert(instanceParam, instanceType),
					property.GetSetMethod(),
					ResolveTypeExpression(propertyResolveFn, property.PropertyType, containerParam)
				),
				instanceParam
			).Compile();

			return obj => { 
				if (getter(obj) == null) setter(obj); 
			};
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
			MethodInfo resolveFn, Type resolveType, Expression containerParam)
		{
			var method = resolveFn.MakeGenericMethod(resolveType);
			return Expression.Call(containerParam, method);
		}

		/// <summary>
		/// Registers the type in the IoC container passed in the constructor and
		/// adds auto-wiring to the specified type.
		/// Auto-wiring is not cached with this method!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void Register<T>()
		{
			var serviceFactory = GenerateAutoWireFn<T>();
			this.container.Register(serviceFactory).ReusedWithin(this.Scope);
		}

		/// <summary>
		/// Registers the type in the IoC container passed in the constructor and
		/// adds auto-wiring to the specified type.
		/// Auto-wiring is not cached with this method!
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TAs"></typeparam>
		public void RegisterAs<T, TAs>() 
			where T : TAs 
		{
			var serviceFactory = GenerateAutoWireFn<T>();

			Func<Container, TAs> fn = c => serviceFactory(c);

			this.container.Register(fn).ReusedWithin(this.Scope);
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
			if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
				return;

			var methodInfo = GetType().GetMethod("RegisterAs", Type.EmptyTypes);
			var registerMethodInfo = methodInfo.MakeGenericMethod(new[] { serviceType, inFunqAsType });
			registerMethodInfo.Invoke(this, new object[0]);

			GenerateServiceFactory(serviceType);
		}

		/// <summary>
		/// Registers the types in the IoC container and
		/// adds auto-wiring to the specified types.
		/// Additionaly the creation of the types is cached when calling <see cref="CreateInstance"/> on the same instance.
		/// </summary>
		/// <param name="serviceTypes"></param>
		public void RegisterTypes(params Type[] serviceTypes)
		{
			RegisterTypes((IEnumerable<Type>) serviceTypes);
		}

		/// <summary>
		/// Registers the types in the IoC container and
		/// adds auto-wiring to the specified types.
		/// Additionaly the creation of the types is cached when calling <see cref="CreateInstance"/> on the same instance.
		/// </summary>
		/// <param name="serviceTypes"></param>
		public void RegisterTypes(IEnumerable<Type> serviceTypes)
		{
			foreach (var serviceType in serviceTypes)
			{
				//Don't try to register base service classes
				if (serviceType.IsAbstract || serviceType.ContainsGenericParameters)
					continue;

				var methodInfo = GetType().GetMethod("Register", Type.EmptyTypes);
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

		/// <summary>
		/// Creates a new auto-wired instance of the specified type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
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