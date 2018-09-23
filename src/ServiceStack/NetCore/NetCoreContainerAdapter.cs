#if NETSTANDARD2_0

using System;
using Microsoft.AspNetCore.Http;
using ServiceStack.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.NetCore
{
    public class NetCoreContainerAdapter : IContainerAdapter, IDisposable
    {
        private readonly IServiceScope scope;
        private readonly IHttpContextAccessor httpContextAccessor;

        public NetCoreContainerAdapter(IServiceProvider appServices)
        {
            httpContextAccessor = appServices.GetService<IHttpContextAccessor>();
            this.scope = appServices.GetService<IServiceScopeFactory>().CreateScope();
        }

        public T TryResolve<T>()
        {
            return httpContextAccessor?.HttpContext != null 
                ? httpContextAccessor.HttpContext.RequestServices.GetService<T>() 
                : scope.ServiceProvider.GetService<T>();
        }

        public T Resolve<T>()
        {
            return httpContextAccessor?.HttpContext != null 
                ? httpContextAccessor.HttpContext.RequestServices.GetRequiredService<T>() 
                : scope.ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}

#endif
