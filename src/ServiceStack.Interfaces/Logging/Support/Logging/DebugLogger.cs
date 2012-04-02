using System;

namespace ServiceStack.Logging.Support.Logging
{
	/// <summary>
	/// Default logger is to System.Diagnostics.Debug.WriteLine
	/// 
	/// Made public so its testable
	/// </summary>
	public class DebugLogger : ILog
	{
		const string DEBUG = "DEBUG: ";
		const string ERROR = "ERROR: ";
		const string FATAL = "FATAL: ";
		const string INFO = "INFO: ";
		const string WARN = "WARN: ";

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugLogger"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public DebugLogger(string type)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugLogger"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public DebugLogger(Type type)
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
			string msg = message == null ? string.Empty : message.ToString();
			if (exception != null)
			{
				msg += ", Exception: " + exception.Message;
			}
			System.Diagnostics.Debug.WriteLine(msg);
		}

		/// <summary>
		/// Logs the format.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="args">The args.</param>
		private static void LogFormat(object message, params object[] args)
		{
			string msg = message == null ? string.Empty : message.ToString();
			System.Diagnostics.Debug.WriteLine(string.Format(msg, args));
		}

		/// <summary>
		/// Logs the specified message.
		/// </summary>
		/// <param name="message">The message.</param>
		private static void Log(object message)
		{
			string msg = message == null ? string.Empty : message.ToString();
			System.Diagnostics.Debug.WriteLine(msg);
		}

		public void Debug(Func<object> messageFunc, Exception exception)
		{
			Log(DEBUG + messageFunc(), exception);
		}

		public void Debug(Func<object> messageFunc)
		{
			Log(DEBUG + messageFunc());
		}

		public void DebugFormat(Func<string> formatFunc, params object[] args)
		{
			LogFormat(DEBUG + formatFunc(), args);
		}

		public void Error(Func<object> messageFunc, Exception exception)
		{
			Log(ERROR + messageFunc(), exception);
		}

		public void Error(Func<object> messageFunc)
		{
			Log(ERROR + messageFunc());
		}

		public void ErrorFormat(Func<string> formatFunc, params object[] args)
		{
			LogFormat(ERROR + formatFunc(), args);
		}

		public void Fatal(Func<object> messageFunc, Exception exception)
		{
			Log(FATAL + messageFunc(), exception);
		}

		public void Fatal(Func<object> messageFunc)
		{
			Log(FATAL + messageFunc());
		}

		public void FatalFormat(Func<string> formatFunc, params object[] args)
		{
			LogFormat(FATAL + formatFunc(), args);
		}

		public void Info(Func<object> messageFunc, Exception exception)
		{
			Log(INFO + messageFunc(), exception);
		}

		public void Info(Func<object> messageFunc)
		{
			Log(INFO + messageFunc());
		}

		public void InfoFormat(Func<string> formatFunc, params object[] args)
		{
			LogFormat(INFO + formatFunc(), args);
		}

		public void Warn(Func<object> messageFunc, Exception exception)
		{
			Log(WARN + messageFunc(), exception);
		}

		public void Warn(Func<object> messageFunc)
		{
			Log(WARN + messageFunc());
		}

		public void WarnFormat(Func<string> formatFunc, params object[] args)
		{
			LogFormat(WARN + formatFunc(), args);
		}

		#endregion
	}
}
