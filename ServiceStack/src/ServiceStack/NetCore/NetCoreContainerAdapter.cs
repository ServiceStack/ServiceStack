#if NETCORE

using System;
using Microsoft.AspNetCore.Http;
using ServiceStack.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack.NetCore;

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
        try
        {
            if (httpContextAccessor?.HttpContext != null)
                return httpContextAccessor.HttpContext.RequestServices.GetService<T>();
            if (scope.ServiceProvider != null)
                return scope.ServiceProvider.GetService<T>();
            return default;
        }
        // treat as not registered, happens with `ValueTask<ICacheClientAsync>`        
        catch (NullReferenceException)
        {
            return default;
        }
        catch (ArgumentNullException)
        {
            return default;
        }
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

#endif
