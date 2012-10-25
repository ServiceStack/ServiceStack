using System;

namespace ServiceStack.Logging.Support.Logging
{
    /// <summary>
    /// Default logger is to System.Diagnostics.Debug.Print
    /// 
    /// Made public so its testable
    /// </summary>
    public class NullDebugLogger : ILog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public NullDebugLogger(string type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
		public NullDebugLogger(Type type)
        {
        }

        #region ILog Members

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        private static void Log(object message, Exception exception)
		{
        }

        /// <summary>
        /// Logs the format.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        private static void LogFormat(object message, params object[] args)
		{
		}

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        private static void Log(object message)
		{
		}

        public void Debug(object message, Exception exception)
		{
		}

		public bool IsDebugEnabled { get { return true; } }

    	public void Debug(object message)
		{
		}

        public void DebugFormat(string format, params object[] args)
		{
		}

        public void Error(object message, Exception exception)
		{
		}

        public void Error(object message)
		{
		}

        public void ErrorFormat(string format, params object[] args)
		{
		}

        public void Fatal(object message, Exception exception)
		{
		}

        public void Fatal(object message)
		{
		}

        public void FatalFormat(string format, params object[] args)
		{
		}

        public void Info(object message, Exception exception)
		{
		}

        public void Info(object message)
		{
		}

        public void InfoFormat(string format, params object[] args)
		{
		}

        public void Warn(object message, Exception exception)
		{
		}

        public void Warn(object message)
		{
		}

        public void WarnFormat(string format, params object[] args)
		{
		}

        #endregion
    }
}
