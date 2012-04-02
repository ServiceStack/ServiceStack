using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Common.Support
{
	/// <summary>
	/// Note: InMemoryLog keeps all logs in memory, so don't use it long running exceptions
	/// 
	/// Returns a thread-safe InMemoryLog which you can use while *TESTING*
	/// to provide a detailed analysis of your logs.
	/// </summary>
	public class InMemoryLogFactory
		: ILogFactory
	{
		public ILog GetLogger(Type type)
		{
			return new InMemoryLog(type.Name);
		}

		public ILog GetLogger(string typeName)
		{
			return new InMemoryLog(typeName);
		}
	}

	public class InMemoryLog
		: ILog
	{
		private readonly object syncLock = new object();
		public string LoggerName { get; private set; }
		public StringBuilder CombinedLog { get; private set; }
		public List<string> DebugEntries { get; set; }
		public List<Exception> DebugExceptions { get; set; }
		public List<string> InfoEntries { get; set; }
		public List<Exception> InfoExceptions { get; set; }
		public List<string> WarnEntries { get; set; }
		public List<Exception> WarnExceptions { get; set; }
		public List<string> ErrorEntries { get; set; }
		public List<Exception> ErrorExceptions { get; set; }
		public List<string> FatalEntries { get; set; }
		public List<Exception> FatalExceptions { get; set; }

		public InMemoryLog(string loggerName)
		{
			this.LoggerName = loggerName;
			this.CombinedLog = new StringBuilder();

			this.DebugEntries = new List<string>();
			this.DebugExceptions = new List<Exception>();
			this.InfoEntries = new List<string>();
			this.InfoExceptions = new List<Exception>();
			this.WarnEntries = new List<string>();
			this.WarnExceptions = new List<Exception>();
			this.ErrorEntries = new List<string>();
			this.ErrorExceptions = new List<Exception>();
			this.FatalEntries = new List<string>();
			this.FatalExceptions = new List<Exception>();
		}

		public bool HasExceptions
		{
			get
			{
				return this.DebugExceptions.Count > 0
					   || this.InfoExceptions.Count > 0
					   || this.WarnExceptions.Count > 0
					   || this.ErrorExceptions.Count > 0
					   || this.FatalExceptions.Count > 0;
			}
		}

		private void AppendToLog(ICollection<string> logEntries, Func<string> formatFunc, params object[] args)
		{
			if (formatFunc == null) return;
			AppendToLog(logEntries, string.Format(formatFunc(), args));
		}

		private void AppendToLog(ICollection<string> logEntries, Func<object> messageFunc)
		{
			if (messageFunc == null) return;
			AppendToLog(logEntries, messageFunc().ToString());
		}

		private void AppendToLog(
			ICollection<string> logEntries, 
			ICollection<Exception> logExceptions, 
			Func<object> messageFunc, Exception ex)
		{
			if (ex != null)
			{
				lock (syncLock)
				{
					logExceptions.Add(ex);
				}
			}
			if (messageFunc == null) return;
			AppendToLog(logEntries, messageFunc().ToString());
		}

		private void AppendToLog(ICollection<string> logEntries, string message)
		{
			lock (this)
			{
				logEntries.Add(message);
				CombinedLog.AppendLine(message);
			}
		}

		public void Debug(Func<object> messageFunc)
		{
			AppendToLog(DebugEntries, messageFunc);
		}

		public void Debug(Func<object> messageFunc, Exception exception)
		{
			AppendToLog(DebugEntries, DebugExceptions, messageFunc, exception);
		}

		public void DebugFormat(Func<string> formatFunc, params object[] args)
		{
			AppendToLog(DebugEntries, formatFunc, args);
		}

		public void Error(Func<object> messageFunc)
		{
			AppendToLog(ErrorEntries, messageFunc);
		}

		public void Error(Func<object> messageFunc, Exception exception)
		{
			AppendToLog(ErrorEntries, ErrorExceptions, messageFunc, exception);
		}

		public void ErrorFormat(Func<string> formatFunc, params object[] args)
		{
			AppendToLog(ErrorEntries, formatFunc, args);
		}

		public void Fatal(Func<object> messageFunc)
		{
			AppendToLog(FatalEntries, messageFunc);
		}

		public void Fatal(Func<object> messageFunc, Exception exception)
		{
			AppendToLog(FatalEntries, FatalExceptions, messageFunc, exception);
		}

		public void FatalFormat(Func<string> formatFunc, params object[] args)
		{
			AppendToLog(FatalEntries, formatFunc, args);
		}

		public void Info(Func<object> messageFunc)
		{
			AppendToLog(InfoEntries, messageFunc);
		}

		public void Info(Func<object> messageFunc, Exception exception)
		{
			AppendToLog(InfoEntries, InfoExceptions, messageFunc, exception);
		}

		public void InfoFormat(Func<string> formatFunc, params object[] args)
		{
			AppendToLog(InfoEntries, formatFunc, args);
		}

		public void Warn(Func<object> messageFunc)
		{
			AppendToLog(WarnEntries, messageFunc);
		}

		public void Warn(Func<object> messageFunc, Exception exception)
		{
			AppendToLog(WarnEntries, WarnExceptions, messageFunc, exception);
		}

		public void WarnFormat(Func<string> formatFunc, params object[] args)
		{
			AppendToLog(WarnEntries, formatFunc, args);
		}

		public bool IsDebugEnabled
		{
			get { return true; }
		}
	}
}