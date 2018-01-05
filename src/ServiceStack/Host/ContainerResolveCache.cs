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
        private readonly Container container;
        private static Dictionary<Type, Func<IResolver, object>> resolveFnMap = new Dictionary<Type, Func<IResolver, object>>();

        public ContainerResolveCache(Container container)
        {
            this.container = container;
        }

        private Func<IResolver, object> GenerateServiceFactory(Type type)
        {
            var containerParam = Expression.Parameter(typeof(IResolver), "container");
            var resolveInstance = Expression.Call(containerParam, "TryResolve", new[] { type });
            var resolveObject = Expression.Convert(resolveInstance, typeof(object));
            return Expression.Lambda<Func<IResolver, object>>(resolveObject, containerParam).Compile();
        }

        /// <summary>
        /// Creates instance using straight Resolve approach.
        /// This will throw an exception if resolution fails
        /// </summary>
        public object CreateInstance(IResolver resolver, Type type)
        {
            return CreateInstance(resolver, type, false);
        }

        /// <summary>
        /// Creates instance using the TryResolve approach if tryResolve = true.
        /// Otherwise uses Resolve approach, which will throw an exception if resolution fails
        /// </summary>
		public object CreateInstance(IResolver resolver, Type type, bool tryResolve)
        {
            Func<IResolver, object> resolveFn;
            if (!resolveFnMap.TryGetValue(type, out resolveFn))
            {
                resolveFn = GenerateServiceFactory(type);

                //Support for multiple threads is needed
                Dictionary<Type, Func<IResolver, object>> snapshot, newCache;
                do
                {
                    snapshot = resolveFnMap;
                    newCache = new Dictionary<Type, Func<IResolver, object>>(resolveFnMap) { [type] = resolveFn };
                } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref resolveFnMap, newCache, snapshot), snapshot));
            }

            var instance = resolveFn(resolver);
            if (instance == null && !tryResolve)
                throw new ResolutionException(type);

            return instance;
        }
    }
}
