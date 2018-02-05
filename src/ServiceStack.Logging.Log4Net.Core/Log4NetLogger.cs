using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace ServiceStack.Logging.Log4Net.Core
{
    public class Log4NetLogger : ILogWithContext, ILogger
    {
        private readonly log4net.ILog _log;
        private Assembly _mainAssembly = Assembly.GetEntryAssembly();

        public Log4NetLogger(string name)
        {
            _log = log4net.LogManager.GetLogger(_mainAssembly, name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public Log4NetLogger(Type type)
        {
            _log = log4net.LogManager.GetLogger(type);
        }

        public bool IsDebugEnabled => _log.IsDebugEnabled;

        /// <summary>
        /// Logs a Debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(object message)
        {
            if (_log.IsDebugEnabled)
                _log.Debug(message);
        }

        /// <summary>
        /// Logs a Debug message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, Exception exception)
        {
            if (_log.IsDebugEnabled)
                _log.Debug(message, exception);
        }

        /// <summary>
        /// Logs a Debug format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Debug(Exception exception, string format, params object[] args)
        {
            if (_log.IsDebugEnabled)
                _log.Debug(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Debug format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void DebugFormat(string format, params object[] args)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat(format, args);
        }

        /// <summary>
        /// Logs a Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(object message)
        {
            if (_log.IsErrorEnabled)
                _log.Error(message);
        }

        /// <summary>
        /// Logs a Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, Exception exception)
        {
            if (_log.IsErrorEnabled)
                _log.Error(message, exception);
        }

        /// <summary>
        /// Logs an Error format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Error(Exception exception, string format, params object[] args)
        {
            if (_log.IsErrorEnabled)
                _log.Error(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            if (_log.IsErrorEnabled)
                _log.ErrorFormat(format, args);
        }

        /// <summary>
        /// Logs a Fatal message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Fatal(object message)
        {
            if (_log.IsFatalEnabled)
                _log.Fatal(message);
        }

        /// <summary>
        /// Logs a Fatal message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, Exception exception)
        {
            if (_log.IsFatalEnabled)
                _log.Fatal(message, exception);
        }

        /// <summary>
        /// Logs a Fatal format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Fatal(Exception exception, string format, params object[] args)
        {
            if (_log.IsFatalEnabled)
                _log.Fatal(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void FatalFormat(string format, params object[] args)
        {
            if (_log.IsFatalEnabled)
                _log.FatalFormat(format, args);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(object message)
        {
            if (_log.IsInfoEnabled)
                _log.Info(message);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, Exception exception)
        {
            if (_log.IsInfoEnabled)
                _log.Info(message, exception);
        }

        /// <summary>
        /// Logs an Info format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Info(Exception exception, string format, params object[] args)
        {
            if (_log.IsInfoEnabled)
                _log.Info(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs an Info format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void InfoFormat(string format, params object[] args)
        {
            if (_log.IsInfoEnabled)
                _log.InfoFormat(format, args);
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
            if (_log.IsWarnEnabled)
                _log.Warn(message);
        }

        /// <summary>
        /// Logs a Warning message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, Exception exception)
        {
            if (_log.IsWarnEnabled)
                _log.Warn(message, exception);
        }

        /// <summary>
        /// Logs a Warn format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Warn(Exception exception, string format, params object[] args)
        {
            if (_log.IsWarnEnabled)
                _log.Warn(string.Format(format, args), exception);
        }

        /// <summary>
        /// Logs a Warning format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void WarnFormat(string format, params object[] args)
        {
            if (_log.IsWarnEnabled)
                _log.WarnFormat(format, args);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <summary>
        /// Check is a logLevel is enabled.
        /// </summary>
        /// <param name="logLevel">the level.</param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return _log.IsFatalEnabled;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return _log.IsDebugEnabled;
                case LogLevel.Error:
                    return _log.IsErrorEnabled;
                case LogLevel.Information:
                    return _log.IsInfoEnabled;
                case LogLevel.Warning:
                    return _log.IsWarnEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        /// <summary>
        /// Logs a message from Logging into Log4Net.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel">The level.</param>
        /// <param name="eventId">The eventId.</param>
        /// <param name="state">The state.</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="formatter">The formatter.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            string message = null;
            if (null != formatter)
            {
                message = formatter(state, exception);
            }
            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Critical:
                        _log.Fatal(message);
                        break;
                    case LogLevel.Debug:
                    case LogLevel.Trace:
                        _log.Debug(message);
                        break;
                    case LogLevel.Error:
                        _log.Error(message);
                        break;
                    case LogLevel.Information:
                        _log.Info(message);
                        break;
                    case LogLevel.Warning:
                        _log.Warn(message);
                        break;
                    default:
                        _log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                        _log.Info(message, exception);
                        break;
                }
            }
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