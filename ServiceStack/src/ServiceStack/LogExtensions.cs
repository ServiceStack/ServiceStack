using System;
using ServiceStack.Logging;

namespace ServiceStack;

public static class LogExtensions
{
    public static void ErrorStrict(this ILog log, string message, Exception ex)
    {
        if (HostContext.StrictMode)
            throw ex;
        log.Error(message, ex);
    }

    public static bool IsNullOrNullLogFactory(this ILogFactory factory)
    {
        return factory == null || factory.GetType() == typeof(NullLogFactory);
    }
}