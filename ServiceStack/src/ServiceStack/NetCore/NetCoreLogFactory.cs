#if NETCORE

using System;
using ServiceStack.Logging;
using Microsoft.Extensions.Logging;

namespace ServiceStack.NetCore;

public class NetCoreLogFactory : ILogFactory
{
    // In test/web watch projects the App can be disposed, disposing the ILoggerFactory 
    // and invalidating all LogFactory instances so try FallbackLoggerFactory which holds the latest ILoggerFactory 
    public static ILoggerFactory FallbackLoggerFactory { get; set; }

    ILoggerFactory loggerFactory;

    public NetCoreLogFactory(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public ILog GetLogger(Type type)
    {
        try
        {
            return new NetCoreLog(loggerFactory.CreateLogger(type));
        }
        catch (ObjectDisposedException)
        {
            if (FallbackLoggerFactory == null) throw;
            try
            {
                loggerFactory = FallbackLoggerFactory;
                return new NetCoreLog(loggerFactory.CreateLogger(type));
            }
            catch (ObjectDisposedException)
            {
                return new NullDebugLogger(type);
            }
        }
    }

    public ILog GetLogger(string typeName)
    {
        try
        {
            return new NetCoreLog(loggerFactory.CreateLogger(typeName));
        }
        catch (ObjectDisposedException)
        {
            if (FallbackLoggerFactory == null) throw;
            try
            {
                loggerFactory = FallbackLoggerFactory;
                return new NetCoreLog(loggerFactory.CreateLogger(typeName));
            }
            catch (ObjectDisposedException)
            {
                return new NullDebugLogger(typeName);
            }
        }
    }
}

public class NetCoreLog(ILogger logger) : ILog, ILogTrace
{
    public ILogger Log { get; } = logger;

    public virtual bool IsTraceEnabled => false;

    public virtual void Trace(object message)
    {
        Log.LogTrace(message.ToString());
    }

    public virtual void Trace(object message, Exception exception)
    {
        Log.LogTrace(default(EventId), exception, message.ToString());
    }

    public virtual void TraceFormat(string format, params object[] args)
    {
        Log.LogTrace(format, args);
    }

    public virtual bool IsDebugEnabled => Log.IsEnabled(LogLevel.Debug);

    public virtual void Debug(object message)
    {        
        Log.LogDebug(message.ToString());
    }

    public virtual void Debug(object message, Exception exception)
    {
        Log.LogDebug(default(EventId), exception, message.ToString());
    }

    public virtual void DebugFormat(string format, params object[] args)
    {
        Log.LogDebug(format, args);
    }

    public virtual void Error(object message)
    {
        if (message is Exception ex)
            Log.LogError(ex, ex.GetType().Name);
        else
            Log.LogError(message.ToString());
    }

    public virtual void Error(object message, Exception exception)
    {
        Log.LogError(default(EventId), exception, message.ToString());
    }

    public virtual void ErrorFormat(string format, params object[] args)
    {
        Log.LogError(format, args);
    }

    public virtual void Fatal(object message)
    {
        if (message is Exception ex)
            Log.LogCritical(ex, ex.GetType().Name);
        else
            Log.LogCritical(message.ToString());
    }

    public virtual void Fatal(object message, Exception exception)
    {
        Log.LogCritical(default(EventId), exception, message.ToString());
    }

    public virtual void FatalFormat(string format, params object[] args)
    {
        Log.LogCritical(format, args);
    }

    public virtual void Info(object message)
    {
        Log.LogInformation(message.ToString());
    }

    public virtual void Info(object message, Exception exception)
    {
        Log.LogInformation(default(EventId), exception, message.ToString());
    }

    public virtual void InfoFormat(string format, params object[] args)
    {
        Log.LogInformation(format, args);
    }

    public virtual void Warn(object message)
    {
        if (message is Exception ex)
            Log.LogWarning(ex, ex.GetType().Name);
        else
            Log.LogWarning(message.ToString());
    }

    public virtual void Warn(object message, Exception exception)
    {
        Log.LogWarning(default(EventId), exception, message.ToString());
    }

    public virtual void WarnFormat(string format, params object[] args)
    {
        Log.LogWarning(format, args);
    }
}


#endif
