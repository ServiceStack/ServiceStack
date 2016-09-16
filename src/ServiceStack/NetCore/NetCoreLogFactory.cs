#if NETSTANDARD1_6

using System;
using ServiceStack.Logging;
using Microsoft.Extensions.Logging;

namespace ServiceStack.NetCore
{
    public class NetCoreLogFactory : ILogFactory
    {
        ILoggerFactory loggerFactory;
        private bool debugEnabled;

        public NetCoreLogFactory(ILoggerFactory loggerFactory, bool debugEnabled=false)
        {
            this.loggerFactory = loggerFactory;
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new NetCoreLog(loggerFactory.CreateLogger(type), debugEnabled);
        }

        public ILog GetLogger(string typeName)
        {
            return new NetCoreLog(loggerFactory.CreateLogger(typeName), debugEnabled);
        }
    }

    public class NetCoreLog : ILog
    {
        private ILogger log;

        public NetCoreLog(ILogger logger, bool debugEnabled=false)
        {
            this.log = logger;
            this.IsDebugEnabled = debugEnabled;
        }

        public bool IsDebugEnabled { get; }

        public void Debug(object message)
        {
            log.LogDebug(null, message.ToString());
        }

        public void Debug(object message, Exception exception)
        {
            log.LogDebug(null, message.ToString(), exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            log.LogDebug(null, format, args);
        }

        public void Error(object message)
        {
            log.LogError(null, message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            log.LogError(null, message.ToString(), exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            log.LogError(null, format, args);
        }

        public void Fatal(object message)
        {
            log.LogCritical(null, message.ToString());
        }

        public void Fatal(object message, Exception exception)
        {
            log.LogCritical(null, message.ToString(), exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            log.LogCritical(null, format, args);
        }

        public void Info(object message)
        {
            log.LogInformation(null, message.ToString());
        }

        public void Info(object message, Exception exception)
        {
            log.LogInformation(null, message.ToString(), exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            log.LogInformation(null, format, args);
        }

        public void Warn(object message)
        {
            log.LogWarning(null, message.ToString());
        }

        public void Warn(object message, Exception exception)
        {
            log.LogWarning(null, message.ToString(), exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            log.LogWarning(null, format, args);
        }
    }
}

#endif
