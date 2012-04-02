using System;
using System.Collections.Generic;

namespace ServiceStack.Logging.Support.Logging {
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

		#region ILog Members

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

		public void Debug(Func<object> messageFunc, Exception exception) {
			Log(Levels.DEBUG, messageFunc(), exception);
		}

		public void Debug(Func<object> messageFunc) {
			Log(Levels.DEBUG, messageFunc());
		}

		public void DebugFormat(Func<string> formatFunc, params object[] args) {
			LogFormat(Levels.DEBUG, formatFunc(), args);
		}

		public void Error(Func<object> messageFunc, Exception exception) {
			Log(Levels.ERROR, messageFunc(), exception);
		}

		public void Error(Func<object> messageFunc) {
			Log(Levels.ERROR, messageFunc());
		}

		public void ErrorFormat(Func<string> formatFunc, params object[] args) {
			LogFormat(Levels.ERROR, formatFunc(), args);
		}

		public void Fatal(Func<object> messageFunc, Exception exception) {
			Log(Levels.FATAL, messageFunc(), exception);
		}

		public void Fatal(Func<object> messageFunc) {
			Log(Levels.FATAL, messageFunc());
		}

		public void FatalFormat(Func<string> formatFunc, params object[] args) {
			LogFormat(Levels.FATAL, formatFunc(), args);
		}

		public void Info(Func<object> messageFunc, Exception exception) {
			Log(Levels.INFO, messageFunc(), exception);
		}

		public void Info(Func<object> messageFunc) {
			Log(Levels.INFO, messageFunc());
		}

		public void InfoFormat(Func<string> formatFunc, params object[] args) {
			LogFormat(Levels.INFO, formatFunc(), args);
		}

		public void Warn(Func<object> messageFunc, Exception exception) {
			Log(Levels.WARN, messageFunc(), exception);
		}

		public void Warn(Func<object> messageFunc) {
			Log(Levels.WARN, messageFunc());
		}

		public void WarnFormat(Func<string> formatFunc, params object[] args) {
			LogFormat(Levels.WARN, formatFunc(), args);
		}

		#endregion
	}
}
