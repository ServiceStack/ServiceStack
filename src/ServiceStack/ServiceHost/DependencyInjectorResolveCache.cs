using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using ServiceStack.DependencyInjection;
using ServiceStack.Configuration;
using System.Linq.Expressions;
using System.Threading;

namespace ServiceStack.ServiceHost
{
    public class DependencyInjectorResolveCache : ITypeFactory
    {
        private DependencyService dependencyService;
        // DAC confirm: Honey I blew up the cache.

        public DependencyInjectorResolveCache(DependencyService dependencyService)
        {
            this.dependencyService = dependencyService;
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
                    return DependencyService.Resolve(type);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return DependencyService.Resolve(type);
            }
        }
    }
}
