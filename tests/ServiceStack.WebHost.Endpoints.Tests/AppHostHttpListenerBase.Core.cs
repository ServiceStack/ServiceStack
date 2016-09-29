//remove this wrapper file when AppHostHttpListener will be implemented on .NET core
#if NETCORE_SUPPORT

namespace ServiceStack
{
    public abstract class AppHostHttpListenerBase : AppSelfHostBase {}

    public abstract class AppHostHttpListenerPoolBase : AppSelfHostBase {}

    public abstract class AppHostHttpListenerSmartPoolBase : AppSelfHostBase {}
}

#endif