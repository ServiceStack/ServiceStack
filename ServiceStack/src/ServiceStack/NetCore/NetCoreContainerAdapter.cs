#if NETCORE

using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using ServiceStack.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Host.NetCore;
using ServiceStack.Logging;

namespace ServiceStack.NetCore;

public class NetCoreContainerAdapter : IContainerAdapter, IDisposable
{
    private readonly IServiceScope scope;
    private readonly IHttpContextAccessor httpContextAccessor;
    private int warnedAboutFallbackScope;
    internal IServiceScopeFactory ScopeFactory { get; }

    public NetCoreContainerAdapter(IServiceProvider appServices)
    {
        httpContextAccessor = appServices.GetService<IHttpContextAccessor>();
        ScopeFactory = appServices.GetRequiredService<IServiceScopeFactory>();
        scope = ScopeFactory.CreateScope();
    }

    private IServiceProvider GetRequestServices() => RequestContext.CurrentRequest switch
    {
        IHasServiceScope { ServiceScope: { } requestScope } => requestScope.ServiceProvider,
        NetCoreRequest request => request.HttpContext.RequestServices,
        _ => httpContextAccessor?.HttpContext?.RequestServices,
    };

    private void WarnAboutFallbackScope()
    {
        if (Interlocked.Exchange(ref warnedAboutFallbackScope, 1) == 0)
        {
            LogManager.GetLogger(typeof(NetCoreContainerAdapter)).Warn(
                "Resolving services from the host-wide fallback scope. Scoped instances resolved here are shared until the ServiceStack host is disposed.");
        }
    }

    public T TryResolve<T>()
    {
        try
        {
            var requestServices = GetRequestServices();
            if (requestServices != null)
                return requestServices.GetService<T>();

            var instance = scope.ServiceProvider.GetService<T>();
            if (instance != null)
                WarnAboutFallbackScope();
            return instance;
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
        var requestServices = GetRequestServices();
        if (requestServices != null)
            return requestServices.GetRequiredService<T>();

        var instance = scope.ServiceProvider.GetRequiredService<T>();
        WarnAboutFallbackScope();
        return instance;
    }

    public void Dispose()
    {
        scope?.Dispose();
    }
}

#endif
