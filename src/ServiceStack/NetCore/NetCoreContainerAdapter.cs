#if NETSTANDARD2_0

using System;
using ServiceStack.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.NetCore
{
    public class NetCoreContainerAdapter : IContainerAdapter
    {
        private readonly IServiceProvider appServices;

        public NetCoreContainerAdapter(IServiceProvider appServices)
        {
            this.appServices = appServices;
        }

        public T TryResolve<T>()
        {
            return appServices.GetService<T>();
        }

        public T Resolve<T>()
        {
            return appServices.GetRequiredService<T>();
        }
    }
}

#endif
