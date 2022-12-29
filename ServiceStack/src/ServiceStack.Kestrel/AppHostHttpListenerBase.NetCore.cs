//Created for compatibility to run tests on .NET Core
#if NETCORE
using System.Reflection;

namespace ServiceStack;

public abstract class AppHostHttpListenerBase : AppSelfHostBase 
{
    protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    { }
}

public abstract class AppHostHttpListenerPoolBase : AppHostHttpListenerBase 
{
    protected AppHostHttpListenerPoolBase(string serviceName, int poolSize, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    { }

    protected AppHostHttpListenerPoolBase(string serviceName, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    { }
}

public abstract class AppHostHttpListenerSmartPoolBase : AppHostHttpListenerPoolBase 
{
    protected AppHostHttpListenerSmartPoolBase(string serviceName, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    { }
}

#endif