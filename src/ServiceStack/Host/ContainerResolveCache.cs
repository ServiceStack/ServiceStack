using System;
using System.Collections.Generic;
using ServiceStack.Configuration;
using Funq;
using System.Linq.Expressions;
using System.Threading;

namespace ServiceStack.Host
{
    public class ContainerResolveCache : ITypeFactory
    {
        private Container container;
        private static Dictionary<Type, Func<Container, object>> resolveFnMap = new Dictionary<Type, Func<Container, object>>();

        public ContainerResolveCache(Container container)
        {
            this.container = container;
        }

        private Func<Container, object> GenerateServiceFactory(Type type, bool tryResolve)
        {
            var resolveType = tryResolve ? "TryResolve" : "Resolve";
            var containerParam = Expression.Parameter(typeof(Container), "container");
            var resolveInstance = Expression.Call(containerParam, resolveType, new[] { type });
            var resolveObject = Expression.Convert(resolveInstance, typeof(object));
            return Expression.Lambda<Func<Container, object>>(resolveObject, containerParam).Compile();
        }

        /// <summary>
        /// Creates instance using straight Resolve approach.
        /// This will throw an exception if resolution fails
        /// </summary>
        public object CreateInstance(Type type)
        {
            return CreateInstance(type, false);
        }

        /// <summary>
        /// Creates instance using the TryResolve approach if tryResolve = true.
        /// Otherwise uses Resolve approach, which will throw an exception if resolution fails
        /// </summary>
		public object CreateInstance(Type type, bool tryResolve)
        {
            Func<Container, object> resolveFn;
            if (!resolveFnMap.TryGetValue(type, out resolveFn))
            {
                resolveFn = GenerateServiceFactory(type, tryResolve);

                //Support for multiple threads is needed
                Dictionary<Type, Func<Container, object>> snapshot, newCache;
                do
                {
                    snapshot = resolveFnMap;
                    newCache = new Dictionary<Type, Func<Container, object>>(resolveFnMap);
                    newCache[type] = resolveFn;
                } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref resolveFnMap, newCache, snapshot), snapshot));
            }

            return resolveFn(this.container);
        }
    }
}
