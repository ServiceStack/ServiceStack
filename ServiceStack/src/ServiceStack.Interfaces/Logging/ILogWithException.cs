namespace ServiceStack.Logging
{
    using System;

    public interface ILogWithException : ILog
    {
        /// <summary>
        /// Logs a Debug format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        void Debug(Exception exception, string format, params object[] args);

        /// <summary>
        /// Logs an Info format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        void Info(Exception exception, string format, params object[] args);

        /// <summary>
        /// Logs a Warn format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        void Warn(Exception exception, string format, params object[] args);

        /// <summary>
        /// Logs an Error format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        void Error(Exception exception, string format, params object[] args);

        /// <summary>
        /// Logs a Fatal format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        void Fatal(Exception exception, string format, params object[] args);
    }

    public static class ILogWithExceptionExtensions
    {
        /// <summary>
        /// Logs a Debug format message and exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Debug(this ILog logger, Exception exception, string format, params object[] args)
        {
            if (logger is ILogWithException exceptionLogger)
            {
                exceptionLogger.Debug(exception, format, args);
            }
            else
            {
                logger.Debug(string.Format(format, args), exception);
            }
        }

        /// <summary>
        /// Logs an Info format message and exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Info(this ILog logger, Exception exception, string format, params object[] args)
        {
            if (logger is ILogWithException exceptionLogger)
            {
                exceptionLogger.Info(exception, format, args);
            }
            else
            {
                logger.Info(string.Format(format, args), exception);
            }
        }

        /// <summary>
        /// Logs a Warn format message and exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Warn(this ILog logger, Exception exception, string format, params object[] args)
        {
            if (logger is ILogWithException exceptionLogger)
            {
                exceptionLogger.Warn(exception, format, args);
            }
            else
            {
                logger.Warn(string.Format(format, args), exception);
            }
        }

        /// <summary>
        /// Logs an Error format message and exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Error(this ILog logger, Exception exception, string format, params object[] args)
        {
            if (logger is ILogWithException exceptionLogger)
            {
                exceptionLogger.Error(exception, format, args);
            }
            else
            {
                logger.Error(string.Format(format, args), exception);
            }
        }

        /// <summary>
        /// Logs a Fatal format message and exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Fatal(this ILog logger, Exception exception, string format, params object[] args)
        {
            if (logger is ILogWithException exceptionLogger)
            {
                exceptionLogger.Fatal(exception, format, args);
            }
            else
            {
                logger.Fatal(string.Format(format, args), exception);
            }
        }
    }
}
