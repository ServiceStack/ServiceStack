using System;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace ServiceStack.Logging.EntLib5
{
    /// <summary>Wrapper over the Enterprise Library 5.0 Logging Application Block</summary>
    public class EntLib5Logger : ILog
    {
        private readonly LogWriter logWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntLib5Logger"/> class.
        /// </summary>
        public EntLib5Logger()
        {
            var factory = new EntLib5Factory();
            logWriter = factory.CreateDefault();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntLib5Logger"/> class.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        public EntLib5Logger(string typeName)
        {
            var factory = new EntLib5Factory();
            logWriter = factory.CreateDefault();
            logWriter.SetContextItem("type", typeName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntLib5Logger"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public EntLib5Logger(Type type)
        {
            var factory = new EntLib5Factory();
            logWriter = factory.CreateDefault();
            logWriter.SetContextItem("type", type.Name);
        }


        /// <summary>
        /// Specifies whether the Debug listener this functional
        /// </summary>
        public bool IsDebugEnabled
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        #region DEBUG Handlers

        /// <summary>
        /// Logs a Debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(object message)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
            else
            {
                if (logWriter.IsLoggingEnabled())
                    WriteMessage(message, TraceEventType.Verbose, null, null);
            }
        }

        /// <summary>
        /// Logs a Debug message and category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        public void Debug(object message, string category)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.WriteLine(message, category);
            }
            else
            {
                if (logWriter.IsLoggingEnabled())
                    WriteMessage(message, TraceEventType.Verbose, category, null);
            }
        }

        /// <summary>
        /// Logs a Debug message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, Exception exception)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
            else
            {
                if (logWriter.IsLoggingEnabled())
                    WriteMessage(message, TraceEventType.Verbose, null, exception);
            }
        }

        /// <summary>
        /// Logs a Debug message, category and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, string category, Exception exception)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.WriteLine(message, category);
            }
            else
            {
                if (logWriter.IsLoggingEnabled())
                    WriteMessage(message, TraceEventType.Verbose, category, exception);
            }
        }

        /// <summary>
        /// Logs a Debug format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
            {
                System.Diagnostics.Debug.Print(format, args);
            }
            else
            {
                throw new NotSupportedException("The DebugFormat method is not supported. Modify the Log Message Format using a formatter in the loggingConfiguration section.");
            }
        }

        #endregion

        #region ERROR Handlers

        /// <summary>
        /// Logs an Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(object message)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Error, null, null);
            }
        }

        /// <summary>
        /// Logs an Error message and category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        public void Error(object message, string category)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Error, category, null);
            }
        }

        /// <summary>
        /// Logs an Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Error, null, exception);
            }
        }

        /// <summary>
        /// Logs an Error message, category and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, string category, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Error, category, exception);
            }
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            if (logWriter.IsLoggingEnabled())
            {
                throw new NotSupportedException("The ErrorFormat method is not supported. Modify the Log Message Format using a formatter in the loggingConfiguration section.");
            }
        }

        #endregion

        #region FATAL Handlers

        /// <summary>
        /// Logs a Fatal Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Fatal(object message)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Critical, null, null);
            }
        }

        /// <summary>
        /// Logs a Fatal Error message and category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        public void Fatal(object message, string category)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Critical, category, null);
            }
        }

        /// <summary>
        /// Logs a Fatal Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Critical, null, exception);
            }
        }

        /// <summary>
        /// Logs a Fatal Error message, category and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, string category, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Critical, category, exception);
            }
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void FatalFormat(string format, params object[] args)
        {
            if (logWriter.IsLoggingEnabled())
            {
                throw new NotSupportedException("The FatalFormat method is not supported. Modify the Log Message Format using a formatter in the loggingConfiguration section.");
            }
        }

        #endregion

        #region INFO Handlers

        /// <summary>
        /// Logs an Info message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(object message)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Information, null, null);
            }
        }

        /// <summary>
        /// Logs an Info message and category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        public void Info(object message, string category)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Information, category, null);
            }
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Information, null, exception);
            }
        }

        /// <summary>
        /// Logs an Info message, category and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, string category, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Information, category, exception);
            }
        }

        /// <summary>
        /// Logs an Info format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void InfoFormat(string format, params object[] args)
        {
            if (logWriter.IsLoggingEnabled())
            {
                throw new NotSupportedException("The InfoFormat method is not supported. Modify the Log Message Format using a formatter in the loggingConfiguration section.");
            }
        }

        #endregion

        #region WARN Handlers

        /// <summary>
        /// Logs a Warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(object message)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Warning, null, null);
            }
        }

        /// <summary>
        /// Logs a Warning message and category.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        public void Warn(object message, string category)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Warning, category, null);
            }
        }

        /// <summary>
        /// Logs a Warning message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Warning, null, exception);
            }
        }

        /// <summary>
        /// Logs a Warning message, category and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, string category, Exception exception)
        {
            if (logWriter.IsLoggingEnabled())
            {
                WriteMessage(message, TraceEventType.Warning, category, exception);
            }
        }

        /// <summary>
        /// Logs a Warning format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void WarnFormat(string format, params object[] args)
        {
            if (logWriter.IsLoggingEnabled())
            {
                throw new NotSupportedException("The WarnFormat method is not supported. Modify the Log Message Format using a formatter in the loggingConfiguration section.");
            }
        }

        #endregion

        private void WriteMessage(object message, TraceEventType eventType, object category = null, Exception exception = null)
        {
            var entry = new LogEntry();
            entry.Severity = eventType;

            //Add Message 
            if (message != null)
                entry.Message = message.ToString();

            //Add category 
            if (category != null)
                entry.Categories.Add(category.ToString());

            //Add exception, user name, detailed date & time if needed 
            entry.ExtendedProperties.Add("Exception", exception);
            entry.TimeStamp = DateTime.UtcNow;

            logWriter.Write(entry);
        }
    }
}
