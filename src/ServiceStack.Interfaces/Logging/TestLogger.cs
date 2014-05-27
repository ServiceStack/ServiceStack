using System;
using System.Collections.Generic;

namespace ServiceStack.Logging 
{
    /// <summary>
    /// Tests logger which  stores all log messages in a member list which can be examined later
    /// 
    /// Made public so its testable
    /// </summary>
    public class TestLogger : ILog {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public TestLogger(string type) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestLogger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public TestLogger(Type type) {
        }

        public enum Levels {
            DEBUG,
            ERROR,
            FATAL,
            INFO,
            WARN,
        };

        static private List<KeyValuePair<Levels, string>> _logs = new List<KeyValuePair<Levels, string>>();


        static public IList<KeyValuePair<Levels, string>> GetLogs() { return _logs; }

        public bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        private static void Log(Levels level, object message, Exception exception) {
            string msg = message == null ? string.Empty : message.ToString();
            if(exception != null) {
                msg += ", Exception: " + exception.Message;
            }
            _logs.Add(new KeyValuePair<Levels, string>(level, msg));
        }

        /// <summary>
        /// Logs the format.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        private static void LogFormat(Levels level, object message, params object[] args) {
            string msg = message == null ? string.Empty : message.ToString();
            _logs.Add(new KeyValuePair<Levels, string>(level, string.Format(msg, args)));
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        private static void Log(Levels level, object message) {
            string msg = message == null ? string.Empty : message.ToString();
            _logs.Add(new KeyValuePair<Levels, string>(level, msg));
        }

        public void Debug(object message, Exception exception) {
            Log(Levels.DEBUG, message, exception);
        }

        public void Debug(object message) {
            Log(Levels.DEBUG, message);
        }

        public void DebugFormat(string format, params object[] args) {
            LogFormat(Levels.DEBUG, format, args);
        }

        public void Error(object message, Exception exception) {
            Log(Levels.ERROR, message, exception);
        }

        public void Error(object message) {
            Log(Levels.ERROR, message);
        }

        public void ErrorFormat(string format, params object[] args) {
            LogFormat(Levels.ERROR, format, args);
        }

        public void Fatal(object message, Exception exception) {
            Log(Levels.FATAL, message, exception);
        }

        public void Fatal(object message) {
            Log(Levels.FATAL, message);
        }

        public void FatalFormat(string format, params object[] args) {
            LogFormat(Levels.FATAL, format, args);
        }

        public void Info(object message, Exception exception) {
            Log(Levels.INFO, message, exception);
        }

        public void Info(object message) {
            Log(Levels.INFO, message);
        }

        public void InfoFormat(string format, params object[] args) {
            LogFormat(Levels.INFO, format, args);
        }

        public void Warn(object message, Exception exception) {
            Log(Levels.WARN, message, exception);
        }

        public void Warn(object message) {
            Log(Levels.WARN, message);
        }

        public void WarnFormat(string format, params object[] args) {
            LogFormat(Levels.WARN, format, args);
        }
    }
}
