using System;
using System.Text;
using ServiceStack.Logging;

namespace PclTest.Android
{
    public class GenericLog : ILog
    {
        const string DEBUG = "DEBUG: ";
        const string ERROR = "ERROR: ";
        const string FATAL = "FATAL: ";
        const string INFO = "INFO: ";
        const string WARN = "WARN: ";

        public Action<string> OnMessage;

        public StringBuilder Logs = new StringBuilder();

        public void LogMessage(string message)
        {
            Logs.AppendLine(message);

            if (OnMessage != null)
                OnMessage(message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceStack.Logging.DebugLogger"/> class.
        /// </summary>
        public GenericLog(Type type) : this(type.Name) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceStack.Logging.DebugLogger"/> class.
        /// </summary>
        public GenericLog(string type)
        {
            IsDebugEnabled = true;
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        private void Log(object message, Exception exception)
        {
            string msg = message == null ? string.Empty : message.ToString();
            if (exception != null)
            {
                msg += ", Exception: " + exception.Message;
            }
            LogMessage(msg);
        }

        /// <summary>
        /// Logs the format.
        /// </summary>
        private void LogFormat(object message, params object[] args)
        {
            string msg = message == null ? string.Empty : message.ToString();
            LogMessage(string.Format(msg, args));
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        private void Log(object message)
        {
            string msg = message == null ? string.Empty : message.ToString();
            LogMessage(msg);
        }

        public void Debug(object message, Exception exception)
        {
            Log(DEBUG + message, exception);
        }

        public bool IsDebugEnabled { get; set; }

        public void Debug(object message)
        {
            Log(DEBUG + message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            LogFormat(DEBUG + format, args);
        }

        public void Error(object message, Exception exception)
        {
            Log(ERROR + message, exception);
        }

        public void Error(object message)
        {
            Log(ERROR + message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            LogFormat(ERROR + format, args);
        }

        public void Fatal(object message, Exception exception)
        {
            Log(FATAL + message, exception);
        }

        public void Fatal(object message)
        {
            Log(FATAL + message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            LogFormat(FATAL + format, args);
        }

        public void Info(object message, Exception exception)
        {
            Log(INFO + message, exception);
        }

        public void Info(object message)
        {
            Log(INFO + message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            LogFormat(INFO + format, args);
        }

        public void Warn(object message, Exception exception)
        {
            Log(WARN + message, exception);
        }

        public void Warn(object message)
        {
            Log(WARN + message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            LogFormat(WARN + format, args);
        }
    }
}