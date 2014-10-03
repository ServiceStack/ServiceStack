using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using DependencyInjection;
using ServiceStack.Configuration;
using Funkee;
using System.Linq.Expressions;
using System.Threading;

namespace ServiceStack.ServiceHost
{
    public class DependencyInjectorResolveCache : ITypeFactory
    {
        private DependencyInjector dependencyInjector;
        // DAC confirm: Honey I blew up the cache.

        public DependencyInjectorResolveCache(DependencyInjector dependencyInjector)
        {
            this.dependencyInjector = dependencyInjector;
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
            if (tryResolve)
            {
                try
                {
                    return dependencyInjector.Resolve(type);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return dependencyInjector.Resolve(type);
            }
        }
    }
}
