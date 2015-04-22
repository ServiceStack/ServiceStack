using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Logging;

namespace ServiceStack.Support
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
        private readonly bool debugEnabled;
        public InMemoryLogFactory(bool debugEnabled = false)
        {
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new InMemoryLog(type.Name) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string typeName)
        {
            return new InMemoryLog(typeName) { IsDebugEnabled = debugEnabled };
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

        private void AppendToLog(ICollection<string> logEntries, string format, params object[] args)
        {
            if (format == null) return;
            AppendToLog(logEntries, string.Format(format, args));
        }

        private void AppendToLog(ICollection<string> logEntries, object message)
        {
            if (message == null) return;
            AppendToLog(logEntries, message.ToString());
        }

        private void AppendToLog(
            ICollection<string> logEntries, 
            ICollection<Exception> logExceptions, 
            object message, Exception ex)
        {
            if (ex != null)
            {
                lock (syncLock)
                {
                    logExceptions.Add(ex);
                }
            }
            if (message == null) return;
            AppendToLog(logEntries, message.ToString());
        }

        private void AppendToLog(ICollection<string> logEntries, string message)
        {
            lock (this)
            {
                logEntries.Add(message);
                CombinedLog.AppendLine(message);
            }
        }

        public void Debug(object message)
        {
            if (IsDebugEnabled)
                AppendToLog(DebugEntries, message);
        }

        public void Debug(object message, Exception exception)
        {
            if (IsDebugEnabled)
                AppendToLog(DebugEntries, DebugExceptions, message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
                AppendToLog(DebugEntries, format, args);
        }

        public void Error(object message)
        {
            AppendToLog(ErrorEntries, message);
        }

        public void Error(object message, Exception exception)
        {
            AppendToLog(ErrorEntries, ErrorExceptions, message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            AppendToLog(ErrorEntries, format, args);
        }

        public void Fatal(object message)
        {
            AppendToLog(FatalEntries, message);
        }

        public void Fatal(object message, Exception exception)
        {
            AppendToLog(FatalEntries, FatalExceptions, message, exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            AppendToLog(FatalEntries, format, args);
        }

        public void Info(object message)
        {
            AppendToLog(InfoEntries, message);
        }

        public void Info(object message, Exception exception)
        {
            AppendToLog(InfoEntries, InfoExceptions, message, exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            AppendToLog(InfoEntries, format, args);
        }

        public void Warn(object message)
        {
            AppendToLog(WarnEntries, message);
        }

        public void Warn(object message, Exception exception)
        {
            AppendToLog(WarnEntries, WarnExceptions, message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            AppendToLog(WarnEntries, format, args);
        }

        public bool IsDebugEnabled { get; set; }
    }
}