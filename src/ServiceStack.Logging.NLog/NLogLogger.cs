using System;
using NLog;
using ServiceStack.Logging;

namespace ServiceStack.Logging.NLogger
{
    /// <summary>
    /// Wrapper over the NLog 2.0 beta and above logger 
    /// </summary>
    public class NLogLogger : ServiceStack.Logging.ILog
    {
        private readonly NLog.Logger log;

        public NLogLogger(string typeName)
        {
            log = NLog.LogManager.GetLogger(typeName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NLogLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public NLogLogger(Type type)
        {
            log = NLog.LogManager.GetLogger(UseFullTypeNames ? type.FullName : type.Name);
        }

        public static bool UseFullTypeNames { get; set; }

        public bool IsDebugEnabled { get { return log.IsDebugEnabled; } }

        public bool IsInfoEnabled { get { return log.IsInfoEnabled; } }

        public bool IsWarnEnabled { get { return log.IsWarnEnabled; } }

        public bool IsErrorEnabled { get { return log.IsErrorEnabled; } }

        public bool IsFatalEnabled { get { return log.IsFatalEnabled; } }
    
        /// <summary>
        /// Logs a Debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(object message)
        {
            if (IsDebugEnabled)
                Write(LogLevel.Debug, message.ToString());
        }

        /// <summary>
        /// Logs a Debug message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, Exception exception)
        {
            if(IsDebugEnabled)
                Write(LogLevel.Debug,exception,message.ToString());
        }

        /// <summary>
        /// Logs a Debug format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
                Write(LogLevel.Debug, format, args);
        }

        /// <summary>
        /// Logs a Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(object message)
        {
            if (IsErrorEnabled)
                Write(LogLevel.Error,message.ToString());
        }

        /// <summary>
        /// Logs a Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, Exception exception)
        {
            if (IsErrorEnabled)
                Write(LogLevel.Error, exception, message.ToString());
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            if (IsErrorEnabled)
                Write(LogLevel.Error,format,args);
        }

        /// <summary>
        /// Logs a Fatal message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Fatal(object message)
        {
            if (IsFatalEnabled)
                Write(LogLevel.Fatal,message.ToString());
        }

        /// <summary>
        /// Logs a Fatal message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, Exception exception)
        {
            if (IsFatalEnabled)
                Write(LogLevel.Fatal, exception, message.ToString());
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void FatalFormat(string format, params object[] args)
        {
            if (IsFatalEnabled)
                Write(LogLevel.Fatal, format, args);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(object message)
        {
            if (IsInfoEnabled)
                Write(LogLevel.Info,message.ToString());
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, Exception exception)
        {
            if (IsInfoEnabled)
                Write(LogLevel.Info,exception,message.ToString());
        }

        /// <summary>
        /// Logs an Info format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void InfoFormat(string format, params object[] args)
        {
            if (IsInfoEnabled)
                Write(LogLevel.Info, format, args);
        }

        /// <summary>
        /// Logs a Warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(object message)
        {
            if (IsWarnEnabled)
                Write(LogLevel.Warn,message.ToString());
        }

        /// <summary>
        /// Logs a Warning message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, Exception exception)
        {
            if (IsWarnEnabled)
                Write(LogLevel.Warn,exception,message.ToString());
        }

        /// <summary>
        /// Logs a Warning format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void WarnFormat(string format, params object[] args)
        {
            if (IsWarnEnabled)
                Write(LogLevel.Warn, format, args);
        }

        private void Write(LogLevel level, string format, params object[] args)
        {
            //preserve call site info - see here: http://stackoverflow.com/questions/3947136/problem-matching-specific-nlog-logger-name
            var logEventInfo = new LogEventInfo(level, log.Name, null, format, args);
            log.Log(typeof(NLogLogger), logEventInfo);
        }

        private void Write(LogLevel level, Exception exception, string format, params object[] args)
        {
            var exceptionEventInfo = new LogEventInfo(level, log.Name, null, format, args, exception);
            log.Log(typeof(NLogLogger), exceptionEventInfo);
        }
    }
}
