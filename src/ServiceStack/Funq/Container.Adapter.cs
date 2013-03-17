using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using ServiceStack.Configuration;
using System;
using ServiceStack.Text;

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
			var serviceFactory = GenerateAutoWireFn<T>();
			return this.Register(serviceFactory);
		}

		/// <summary>
		/// Register an autowired dependency as a separate type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public IRegistration<TAs> RegisterAutoWiredAs<T, TAs>()
			where T : TAs
		{
			var serviceFactory = GenerateAutoWireFn<T>();
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
			AutoWire(this, instance);
		}


        private Dictionary<Type, Action<object>[]> autoWireCache = new Dictionary<Type, Action<object>[]>();

        private static MethodInfo GetResolveMethod(Type typeWithResolveMethod, Type serviceType)
        {
            var methodInfo = typeWithResolveMethod.GetMethod("Resolve", new Type[0]);
            return methodInfo.MakeGenericMethod(new[] { serviceType });
        }

        public static ConstructorInfo GetConstructorWithMostParams(Type type)
        {
            return type.GetConstructors()
                .OrderByDescending(x => x.GetParameters().Length)
                .FirstOrDefault(ctor => !ctor.IsStatic);
        }

        /// <summary>
        /// Generates a function which creates and auto-wires <see cref="TService"/>.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="lambdaParam"></param>
        /// <returns></returns>
        public static Func<Container, TService> GenerateAutoWireFn<TService>()
        {
            var lambdaParam = Expression.Parameter(typeof(Container), "container");
            var propertyResolveFn = typeof(Container).GetMethod("TryResolve", new Type[0]);
            var memberBindings = typeof(TService).GetPublicProperties()
                .Where(x => x.CanWrite && !x.PropertyType.IsValueType && x.PropertyType != typeof(string))
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
        public void AutoWire(Container container, object instance)
        {
            var instanceType = instance.GetType();
            var propertyResolveFn = typeof(Container).GetMethod("TryResolve", new Type[0]);

            Action<object>[] setters;
            if (!autoWireCache.TryGetValue(instanceType, out setters))
            {
                setters = instanceType.GetPublicProperties()
                    .Where(x => x.CanWrite && !x.PropertyType.IsValueType && x.PropertyType != typeof(string))
                    .Select(x => GenerateAutoWireFnForProperty(container, propertyResolveFn, x, instanceType))
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

        private static Action<object> GenerateAutoWireFnForProperty(
            Container container, MethodInfo propertyResolveFn, PropertyInfo property, Type instanceType)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var containerParam = Expression.Constant(container);

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

            return obj =>
            {
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
    }

}