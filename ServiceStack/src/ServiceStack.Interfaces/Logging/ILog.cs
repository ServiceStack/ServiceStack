using ServiceStack.Messaging;
using System;

namespace ServiceStack.Logging;

/// <summary>
/// Logs a message in a running application
/// </summary>
public interface ILog 
{
    /// <summary>
    /// Gets or sets a value indicating whether this instance is debug enabled.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is debug enabled; otherwise, <c>false</c>.
    /// </value>
    bool IsDebugEnabled { get; }
    
    /// <summary>
    /// Logs a Debug message.
    /// </summary>
    /// <param name="message">The message.</param>
    void Debug(object message);

    /// <summary>
    /// Logs a Debug message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    void Debug(object message, Exception exception);

    /// <summary>
    /// Logs a Debug format message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    [JetBrains.Annotations.StringFormatMethod("format")]
    void DebugFormat(string format, params object[] args);

    /// <summary>
    /// Logs a Error message.
    /// </summary>
    /// <param name="message">The message.</param>
    void Error(object message);

    /// <summary>
    /// Logs a Error message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    void Error(object message, Exception exception);

    /// <summary>
    /// Logs a Error format message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    [JetBrains.Annotations.StringFormatMethod("format")]
    void ErrorFormat(string format, params object[] args);

    /// <summary>
    /// Logs a Fatal message.
    /// </summary>
    /// <param name="message">The message.</param>
    void Fatal(object message);

    /// <summary>
    /// Logs a Fatal message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    void Fatal(object message, Exception exception);

    /// <summary>
    /// Logs a Error format message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    [JetBrains.Annotations.StringFormatMethod("format")]
    void FatalFormat(string format, params object[] args);

    /// <summary>
    /// Logs an Info message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    void Info(object message);

    /// <summary>
    /// Logs an Info message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    void Info(object message, Exception exception);

    /// <summary>
    /// Logs an Info format message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    [JetBrains.Annotations.StringFormatMethod("format")]
    void InfoFormat(string format, params object[] args);

    /// <summary>
    /// Logs a Warning message.
    /// </summary>
    /// <param name="message">The message.</param>
    void Warn(object message);

    /// <summary>
    /// Logs a Warning message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    void Warn(object message, Exception exception);

    /// <summary>
    /// Logs a Warning format message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    [JetBrains.Annotations.StringFormatMethod("format")]
    void WarnFormat(string format, params object[] args);
}

/// <summary>
/// When implemented will log as TRACE otherwise as DEBUG
/// </summary>
public interface ILogTrace
{
    /// <summary>
    /// Gets or sets a value indicating whether this instance is trace enabled.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is trace enabled; otherwise, <c>false</c>.
    /// </value>
    bool IsTraceEnabled { get; }

    /// <summary>
    /// Logs a Trace message.
    /// </summary>
    /// <param name="message">The message.</param>
    void Trace(object message);

    /// <summary>
    /// Logs a Trace message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    void Trace(object message, Exception exception);

    /// <summary>
    /// Logs a Trace format message.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    [JetBrains.Annotations.StringFormatMethod("format")]
    void TraceFormat(string format, params object[] args);
}


public static class LogUtils
{
    public static bool IsTraceEnabled(this ILog log) => log is ILogTrace traceLog 
        ? traceLog.IsTraceEnabled 
        : log.IsDebugEnabled;

    public static void Trace(this ILog log, object message)
    {
        if (log is ILogTrace traceLog)
            traceLog.Trace(message);
        else
            log.Debug(message);
    }

    public static void Trace(this ILog log, object message, Exception exception)
    {
        if (log is ILogTrace traceLog)
            traceLog.Trace(message, exception);
        else
            log.Debug(message, exception);
    }

    public static void TraceFormat(this ILog log, string format, params object[] args)
    {
        if (log is ILogTrace traceLog)
            traceLog.TraceFormat(format, args);
        else
            log.DebugFormat(format, args);
    }
}
