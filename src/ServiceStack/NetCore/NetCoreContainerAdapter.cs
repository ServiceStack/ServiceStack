#if NETSTANDARD2_0

using System;
using ServiceStack.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.NetCore
{
    public class NetCoreContainerAdapter : IContainerAdapter, IDisposable
    {
        private readonly IServiceScope scope;

        public NetCoreContainerAdapter(IServiceProvider appServices)
        {
            this.scope = appServices.GetService<IServiceScopeFactory>().CreateScope();
        }

        public T TryResolve<T>()
        {
            return scope.ServiceProvider.GetService<T>();
        }

        public T Resolve<T>()
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}

#endif
