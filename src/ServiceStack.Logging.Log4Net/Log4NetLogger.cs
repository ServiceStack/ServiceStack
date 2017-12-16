using System;

namespace ServiceStack.Logging.Log4Net
{
    /// <summary>
    /// Wrapper over the log4net.1.2.10 and above logger 
    /// </summary>
	public class Log4NetLogger : ILogWithContext
    {
        private readonly log4net.ILog log;

        public Log4NetLogger(string typeName)
        {
            log = log4net.LogManager.GetLogger(typeName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public Log4NetLogger(Type type)
        {
            log = log4net.LogManager.GetLogger(type);
        }

        public bool IsDebugEnabled => log.IsDebugEnabled;

        /// <summary>
        /// Logs a Debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(object message)
        {
            if (log.IsDebugEnabled)
                log.Debug(message);
        }

        /// <summary>
        /// Logs a Debug message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, Exception exception)
        {
            if (log.IsDebugEnabled)
                log.Debug(message, exception);
        }

        /// <summary>
        /// Logs a Debug format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Debug(Exception exception, string format, params object[] args)
        {
            if (log.IsDebugEnabled)
                log.Debug(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Debug format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void DebugFormat(string format, params object[] args)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat(format, args);
        }

        /// <summary>
        /// Logs a Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(object message)
        {
            if (log.IsErrorEnabled)
                log.Error(message);
        }

        /// <summary>
        /// Logs a Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, Exception exception)
        {
            if (log.IsErrorEnabled)
                log.Error(message, exception);
        }

        /// <summary>
        /// Logs an Error format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Error(Exception exception, string format, params object[] args)
        {
            if (log.IsErrorEnabled)
                log.Error(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            if (log.IsErrorEnabled)
                log.ErrorFormat(format, args);
        }

        /// <summary>
        /// Logs a Fatal message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Fatal(object message)
        {
            if (log.IsFatalEnabled)
                log.Fatal(message);
        }

        /// <summary>
        /// Logs a Fatal message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, Exception exception)
        {
            if (log.IsFatalEnabled)
                log.Fatal(message, exception);
        }

        /// <summary>
        /// Logs a Fatal format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Fatal(Exception exception, string format, params object[] args)
        {
            if (log.IsFatalEnabled)
                log.Fatal(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void FatalFormat(string format, params object[] args)
        {
            if (log.IsFatalEnabled)
                log.FatalFormat(format, args);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(object message)
        {
            if (log.IsInfoEnabled)
                log.Info(message);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, Exception exception)
        {
            if (log.IsInfoEnabled)
                log.Info(message, exception);
        }

        /// <summary>
        /// Logs an Info format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Info(Exception exception, string format, params object[] args)
        {
            if (log.IsInfoEnabled)
                log.Info(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs an Info format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void InfoFormat(string format, params object[] args)
        {
            if (log.IsInfoEnabled)
                log.InfoFormat(format, args);
        }

        public IDisposable PushProperty(string key, object value)
        {
            log4net.LogicalThreadContext.Properties[key] = value;
            return new RemovePropertyOnDispose(key);
        }

        /// <summary>
        /// Logs a Warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(object message)
        {
            if (log.IsWarnEnabled)
                log.Warn(message);
        }

        /// <summary>
        /// Logs a Warning message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, Exception exception)
        {
            if (log.IsWarnEnabled)
                log.Warn(message, exception);
        }

        /// <summary>
        /// Logs a Warn format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Warn(Exception exception, string format, params object[] args)
        {
            if (log.IsWarnEnabled)
                log.Warn(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Warning format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void WarnFormat(string format, params object[] args)
        {
            if (log.IsWarnEnabled)
                log.WarnFormat(format, args);
        }

        private class RemovePropertyOnDispose : IDisposable
        {
            private readonly string removeKey;

            public RemovePropertyOnDispose(string removeKey)
            {
                this.removeKey = removeKey;
            }

            public void Dispose()
            {
                log4net.LogicalThreadContext.Properties.Remove(removeKey);
            }
        }
    }
}
