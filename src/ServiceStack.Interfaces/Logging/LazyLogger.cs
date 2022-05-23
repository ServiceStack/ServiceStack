using System;

namespace ServiceStack.Logging;

public class LazyLogger : ILog
{
    public Type Type { get; }

    public LazyLogger(Type type) => Type = type;
    public bool IsDebugEnabled => LogManager.GetLogger(Type).IsDebugEnabled;
    
    public void Debug(object message) => LogManager.LogFactory.GetLogger(Type).Debug(message);

    public void Debug(object message, Exception exception) => LogManager.LogFactory.GetLogger(Type).Debug(message, exception);

    public void DebugFormat(string format, params object[] args) => LogManager.LogFactory.GetLogger(Type).DebugFormat(format, args);

    public void Error(object message) => LogManager.LogFactory.GetLogger(Type).Error(message);

    public void Error(object message, Exception exception) => LogManager.LogFactory.GetLogger(Type).Error(message, exception);

    public void ErrorFormat(string format, params object[] args) => LogManager.LogFactory.GetLogger(Type).ErrorFormat(format, args);

    public void Fatal(object message) => LogManager.LogFactory.GetLogger(Type).Fatal(message);

    public void Fatal(object message, Exception exception) => LogManager.LogFactory.GetLogger(Type).Fatal(message, exception);

    public void FatalFormat(string format, params object[] args) => LogManager.LogFactory.GetLogger(Type).FatalFormat(format, args);

    public void Info(object message) => LogManager.LogFactory.GetLogger(Type).Info(message);

    public void Info(object message, Exception exception) => LogManager.LogFactory.GetLogger(Type).Info(message, exception);

    public void InfoFormat(string format, params object[] args) => LogManager.LogFactory.GetLogger(Type).InfoFormat(format, args);

    public void Warn(object message) => LogManager.LogFactory.GetLogger(Type).Warn(message);

    public void Warn(object message, Exception exception) => LogManager.LogFactory.GetLogger(Type).Warn(message, exception);

    public void WarnFormat(string format, params object[] args) => LogManager.LogFactory.GetLogger(Type).WarnFormat(format, args);
}