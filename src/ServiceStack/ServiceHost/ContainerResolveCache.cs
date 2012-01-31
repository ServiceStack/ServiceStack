using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Configuration;
using Funq;
using System.Linq.Expressions;
using System.Threading;

namespace ServiceStack.ServiceHost
{
	public class ContainerResolveCache : ITypeFactory
	{
		private Container container;
		private static Dictionary<Type, Func<object>> resolveFnMap = new Dictionary<Type, Func<object>>();

		public ContainerResolveCache(Container container)
		{
			this.container = container;
		}

		private Func<object> GenerateServiceFactory(Type type)
		{
			var containerInstance = Expression.Constant(this.container);
			var resolveInstance = Expression.Call(containerInstance, "Resolve", new[] { type }, new Expression[0]);
			var resolveObject = Expression.Convert(resolveInstance, typeof(object));
			return Expression.Lambda<Func<object>>(resolveObject, new ParameterExpression[0]).Compile();
		}

		public object CreateInstance(Type type)
		{
			Func<object> resolveFn;
			if (!resolveFnMap.TryGetValue(type, out resolveFn))
			{
				resolveFn = GenerateServiceFactory(type);

				//Support for multiple threads is needed
				Dictionary<Type, Func<object>> snapshot, newCache;
				do
				{
					snapshot = resolveFnMap;
					newCache = new Dictionary<Type, Func<object>>(resolveFnMap);
					newCache[type] = resolveFn;
				} while (!ReferenceEquals(
				Interlocked.CompareExchange(ref resolveFnMap, newCache, snapshot), snapshot));
			}

			return resolveFn();
		}
	}
}
