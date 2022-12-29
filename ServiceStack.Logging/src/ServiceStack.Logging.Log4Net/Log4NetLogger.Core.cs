#if NETSTANDARD2_0

using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace ServiceStack.Logging.Log4Net
{
    public partial class Log4NetLogger : ILogger
    {
        private Assembly mainAssembly = Assembly.GetEntryAssembly();

        public Log4NetLogger(string name)
        {
            log = log4net.LogManager.GetLogger(mainAssembly, name);
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
                    return log.IsFatalEnabled;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return log.IsDebugEnabled;
                case LogLevel.Error:
                    return log.IsErrorEnabled;
                case LogLevel.Information:
                    return log.IsInfoEnabled;
                case LogLevel.Warning:
                    return log.IsWarnEnabled;
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
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Critical:
                        log.Fatal(message, exception);
                        break;
                    case LogLevel.Debug:
                    case LogLevel.Trace:
                        log.Debug(message, exception);
                        break;
                    case LogLevel.Error:
                        log.Error(message, exception);
                        break;
                    case LogLevel.Information:
                        log.Info(message, exception);
                        break;
                    case LogLevel.Warning:
                        log.Warn(message, exception);
                        break;
                    default:
                        log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                        log.Info(message, exception);
                        break;
                }
            }
        }
    }
}

#endif