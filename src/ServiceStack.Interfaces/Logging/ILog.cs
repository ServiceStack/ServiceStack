using System;

namespace ServiceStack.Logging
{
	/// <summary>
	/// Logs a message in a running application
	/// </summary>
	public interface ILog 
	{
		/// <summary>
		/// Logs a Debug message
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		void Debug(Func<object> messageFunc);

		/// <summary>
		/// Logs a Debug message and exception.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="exception">The exception.</param>
		void Debug(Func<object> messageFunc, Exception exception);

		/// <summary>
		/// Logs a Debug format message.
		/// </summary>
		/// <param name="formatFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="args">The args.</param>
		void DebugFormat(Func<string> formatFunc, params object[] args);

		/// <summary>
		/// Logs a Error message.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		void Error(Func<object> messageFunc);

		/// <summary>
		/// Logs a Error message and exception.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="exception">The exception.</param>
		void Error(Func<object> messageFunc, Exception exception);

		/// <summary>
		/// Logs a Error format message.
		/// </summary>
		/// <param name="formatFunc">A function returning a string to be formatted and written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="args">The args.</param>
		void ErrorFormat(Func<string> formatFunc, params object[] args);

		/// <summary>
		/// Logs a Fatal message.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		void Fatal(Func<object> messageFunc);

		/// <summary>
		/// Logs a Fatal message and exception.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="exception">The exception.</param>
		void Fatal(Func<object> messageFunc, Exception exception);

		/// <summary>
		/// Logs a Error format message.
		/// </summary>
		/// <param name="formatFunc">A function returning a string to be formatted and written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="args">The args.</param>
		void FatalFormat(Func<string> formatFunc, params object[] args);

		/// <summary>
		/// Logs an Info message and exception.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		void Info(Func<object> messageFunc);

		/// <summary>
		/// Logs an Info message and exception.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="exception">The exception.</param>
		void Info(Func<object> messageFunc, Exception exception);

		/// <summary>
		/// Logs an Info format message.
		/// </summary>
		/// <param name="formatFunc">A function returning a string to be formatted and written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="args">The args.</param>
		void InfoFormat(Func<string> formatFunc, params object[] args);

		/// <summary>
		/// Logs a Warning message.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		void Warn(Func<object> messageFunc);

		/// <summary>
		/// Logs a Warning message and exception.
		/// </summary>
		/// <param name="messageFunc">A function returning message to be written. Function is not evaluated if logging is not enabled.</param>
		/// <param name="exception">The exception.</param>
		void Warn(Func<object> messageFunc, Exception exception);

		/// <summary>
		/// Logs a Warning format message.
		/// </summary>
		/// <param name="formatFunc">The format.</param>
		/// <param name="args">The args.</param>
		void WarnFormat(Func<string> formatFunc, params object[] args);
	}
}
