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

public class NetCoreLog : ILog
{
    public ILogger Log { get; }

    public NetCoreLog(ILogger logger)
    {
        this.Log = logger;
    }

    public bool IsDebugEnabled => Log.IsEnabled(LogLevel.Debug);

    public void Debug(object message)
    {        
        Log.LogDebug(message.ToString());
    }

    public void Debug(object message, Exception exception)
    {
        Log.LogDebug(default(EventId), exception, message.ToString());
    }

    public void DebugFormat(string format, params object[] args)
    {
        Log.LogDebug(format, args);
    }

    public void Error(object message)
    {
        Log.LogError(message.ToString());
    }

    public void Error(object message, Exception exception)
    {
        Log.LogError(default(EventId), exception, message.ToString());
    }

    public void ErrorFormat(string format, params object[] args)
    {
        Log.LogError(format, args);
    }

    public void Fatal(object message)
    {
        Log.LogCritical(message.ToString());
    }

    public void Fatal(object message, Exception exception)
    {
        Log.LogCritical(default(EventId), exception, message.ToString());
    }

    public void FatalFormat(string format, params object[] args)
    {
        Log.LogCritical(format, args);
    }

    public void Info(object message)
    {
        Log.LogInformation(message.ToString());
    }

    public void Info(object message, Exception exception)
    {
        Log.LogInformation(default(EventId), exception, message.ToString());
    }

    public void InfoFormat(string format, params object[] args)
    {
        Log.LogInformation(format, args);
    }

    public void Warn(object message)
    {
        Log.LogWarning(message.ToString());
    }

    public void Warn(object message, Exception exception)
    {
        Log.LogWarning(default(EventId), exception, message.ToString());
    }

    public void WarnFormat(string format, params object[] args)
    {
        Log.LogWarning(format, args);
    }
}


#endif
