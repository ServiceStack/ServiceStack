using System;
using System.Diagnostics;
using System.Text;

namespace ServiceStack.Logging.EventLog
{
    /// <summary>
    /// ILog used to write to the Event Log
    /// </summary>
    public class EventLogger : ILogWithException
    {
        private const string NEW_LINE = "\r\n\r\n";
        public static string ERROR_MSG = "An error occurred in the application: {0}\r\nException: {1}";

        private readonly string eventLogSource;
        private readonly string eventLogName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogger"/> class.
        /// </summary>
        /// <param name="eventLogName">Name of the event log.</param>
        /// <param name="eventLogSource">The event log source.</param>
        public EventLogger(string eventLogName, string eventLogSource)
        {
            if (string.IsNullOrEmpty(eventLogName))
                throw new ArgumentNullException(nameof(eventLogName));

            if (string.IsNullOrEmpty(eventLogSource))
                throw new ArgumentNullException(nameof(eventLogSource));

            this.eventLogName = eventLogName;
            this.eventLogSource = eventLogSource;
        }

        public bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Writes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="eventLogType">Type of the event log.</param>
        private void Write(object message, EventLogEntryType eventLogType)
        {
            Write(message, null, eventLogType);
        }

        /// <summary>
        /// Writes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exeception">The execption.</param>
        /// <param name="eventLogType">Type of the event log.</param>
        private void Write(object message, Exception exeception, EventLogEntryType eventLogType)
        {
            var sb = new StringBuilder();

            System.Diagnostics.EventLog eventLogger = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists(eventLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(eventLogSource, eventLogName);
            }

            sb.Append(message).Append(NEW_LINE);
            while (exeception != null)
            {
                sb.Append("Message: ").Append(exeception.Message).Append(NEW_LINE)
                .Append("Source: ").Append(exeception.Source).Append(NEW_LINE)
                .Append("Target site: ").Append(exeception.TargetSite).Append(NEW_LINE)
                .Append("Stack trace: ").Append(exeception.StackTrace).Append(NEW_LINE);

                // Walk the InnerException tree
                exeception = exeception.InnerException;
            }

            eventLogger.Source = eventLogSource;
            eventLogger.WriteEntry(String.Format(ERROR_MSG, eventLogName, sb), eventLogType);
        }

        /// <summary>
        /// Logs a Debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(object message)
        {
            if (IsDebugEnabled)
                Write("DEBUG: " + message, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a Debug message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, Exception exception)
        {
            if (IsDebugEnabled)
                Write("DEBUG: " + message, exception, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a Debug format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
                Write("DEBUG: " + string.Format(format, args), EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a Debug format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Debug(Exception exception, string format, params object[] args)
        {
            if (IsDebugEnabled)
                Write("DEBUG: " + string.Format(format, args), exception, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(object message)
        {
            Write("ERROR: " + message, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs a Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, Exception exception)
        {
            Write("ERROR: " + message, exception, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            Write("ERROR: " + string.Format(format, args), EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs an Error format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Error(Exception exception, string format, params object[] args)
        {
            Write("ERROR: " + string.Format(format, args), exception, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs a Fatal message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Fatal(object message)
        {
            Write("FATAL: " + message, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs a Fatal message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, Exception exception)
        {
            Write("FATAL: " + message, exception, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void FatalFormat(string format, params object[] args)
        {
            Write("FATAL: " + string.Format(format, args), EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs a Fatal format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Fatal(Exception exception, string format, params object[] args)
        {
            Write("FATAL: " + string.Format(format, args), exception, EventLogEntryType.Error);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(object message)
        {
            Write("INFO: " + message, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, Exception exception)
        {
            Write("INFO: " + message, exception, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs an Info format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void InfoFormat(string format, params object[] args)
        {
            Write("INFO: " + string.Format(format, args), EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs an Info format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Info(Exception exception, string format, params object[] args)
        {
            Write("INFO: " + string.Format(format, args), exception, EventLogEntryType.Information);
        }

        /// <summary>
        /// Logs a Warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(object message)
        {
            Write("WARN: " + message, EventLogEntryType.Warning);
        }

        /// <summary>
        /// Logs a Warning message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, Exception exception)
        {
            Write("WARN: " + message, exception, EventLogEntryType.Warning);
        }

        /// <summary>
        /// Logs a Warning format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void WarnFormat(string format, params object[] args)
        {
            Write("WARN: " + string.Format(format, args), EventLogEntryType.Warning);
        }

        /// <summary>
        /// Logs a Warn format message and exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Warn(Exception exception, string format, params object[] args)
        {
            Write("WARN: " + string.Format(format, args), exception, EventLogEntryType.Warning);
        }
    }
}
