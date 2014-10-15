using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;

namespace ServiceStack.DependencyInjection
{
    public class DependencyResolver : IDisposable
    {
        private readonly ILifetimeScope _lifetimeScope;

        public DependencyResolver(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public object Resolve(Type type)
        {
            return _lifetimeScope.Resolve(type);
        }

        public T TryResolve<T>()
        {
            try
            {
                return _lifetimeScope.Resolve<T>();
            }
            catch (DependencyResolutionException unusedException)
            {
                return default(T);
            }
        }

        public void Dispose()
        {
            _lifetimeScope.Dispose();
        }
    }
}
