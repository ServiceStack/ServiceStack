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

		public void Debug(Func<object> messageFunc, Exception exception)
		{
		}

		public void Debug(Func<object> messageFunc)
		{
		}

		public void DebugFormat(Func<string> formatFunc, params object[] args)
		{
		}

		public void Error(Func<object> messageFunc, Exception exception)
		{
		}

		public void Error(Func<object> messageFunc)
		{
		}

		public void ErrorFormat(Func<string> formatFunc, params object[] args)
		{
		}

		public void Fatal(Func<object> messageFunc, Exception exception)
		{
		}

		public void Fatal(Func<object> messageFunc)
		{
		}

		public void FatalFormat(Func<string> formatFunc, params object[] args)
		{
		}

		public void Info(Func<object> messageFunc, Exception exception)
		{
		}

		public void Info(Func<object> messageFunc)
		{
		}

		public void InfoFormat(Func<string> formatFunc, params object[] args)
		{
		}

		public void Warn(Func<object> messageFunc, Exception exception)
		{
		}

		public void Warn(Func<object> messageFunc)
		{
		}

		public void WarnFormat(Func<string> formatFunc, params object[] args)
		{
		}

		#endregion
	}
}
