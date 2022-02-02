using System;
using System.Text;

namespace ServiceStack.Logging
{
    /// <summary>
    /// StringBuilderLog writes to shared StringBuffer.
    /// Made public so its testable
    /// </summary>
    public class StringBuilderLogFactory : ILogFactory
    {
        private readonly StringBuilder sb;
        private readonly bool debugEnabled;

        public StringBuilderLogFactory(bool debugEnabled = true)
        {
            sb = new StringBuilder();
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new StringBuilderLog(type, sb) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string typeName)
        {
            return new StringBuilderLog(typeName, sb) { IsDebugEnabled = debugEnabled };
        }

        public string GetLogs()
        {
            lock (sb)
                return sb.ToString();
        }

        public void ClearLogs()
        {
            lock (sb)
                sb.Remove(0, sb.Length - 1);
        }
    }

    public class StringBuilderLog : ILog
    {
        const string DEBUG = "DEBUG: ";
        const string ERROR = "ERROR: ";
        const string FATAL = "FATAL: ";
        const string INFO = "INFO: ";
        const string WARN = "WARN: ";
        private readonly StringBuilder logs;

        public StringBuilderLog(string type, StringBuilder logs)
        {
            this.logs = logs;
        }

        public StringBuilderLog(Type type, StringBuilder logs)
        {
            this.logs = logs;
        }

        public bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        private void Log(object message, Exception exception)
        {
            var msg = message?.ToString() ?? string.Empty;
            if (exception != null)
            {
                msg += ", Exception: " + exception.Message;
            }
            lock (logs)
                logs.AppendLine(msg);
        }

        /// <summary>
        /// Logs the format.
        /// </summary>
        private void LogFormat(object message, params object[] args)
        {
            var msg = message?.ToString() ?? string.Empty;
            lock (logs)
            {
                logs.AppendFormat(msg, args);
                logs.AppendLine();
            }
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void Log(object message)
        {
            var msg = message?.ToString() ?? string.Empty;
            lock (logs)
            {
                logs.AppendLine(msg);
            }
        }

        public void Debug(object message, Exception exception)
        {
            Log(DEBUG + message, exception);
        }

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
