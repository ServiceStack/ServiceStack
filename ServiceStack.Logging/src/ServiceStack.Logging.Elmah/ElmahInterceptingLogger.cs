using Elmah;
using System;
using System.Web;

namespace ServiceStack.Logging.Elmah
{
    /// <summary>	Writes Elmah intercepting logger.  </summary>
    /// <remarks>	9/2/2011. </remarks>
    public class ElmahInterceptingLogger
        : ILog
    {
        private readonly ILog log;
        private readonly HttpApplication application;

        /// <summary>	Constructor. </summary>
        /// <remarks>
        /// Logs to the given Elmah ErrorLog.  Only Error and Fatal are passed along to Elmah, while all other errors will be written to the
        /// wrapped logger.
        /// </remarks>
        /// <exception cref="ArgumentNullException">	Thrown when either the wrapped ILog or Elmah ErrorLog are null. </exception>
        /// <param name="log">	   	The underlying log to write to. </param>
        /// <param name="application"> The application to signal with the errors </param>
        public ElmahInterceptingLogger(ILog log, HttpApplication application)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.application = application ?? throw new ArgumentNullException(nameof(application));
        }

        public void Debug(object message, Exception exception)
        {
            log.Debug(message, exception);
        }

        public void Debug(object message)
        {
            log.Debug(message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            log.DebugFormat(format, args);
        }

        private void RaiseError(Exception exception)
        {
            if (exception == null)
                return;

            try
            {
                var signal = ErrorSignal.Get(application);
                signal?.Raise(exception);
            }
            catch
            {
                // Elmah signal failures should not prevent underlying logging
            }
        }

        private void RaiseError(object message)
        {
            var str = message?.ToString() ?? "(null)";
            RaiseError(new System.ApplicationException(str));
        }

        private void RaiseError(string format, params object[] args)
        {
            try
            {
                var str = string.Format(format, args);
                RaiseError(new System.ApplicationException(str));
            }
            catch (Exception ex)
            {
                RaiseError(ex);
            }
        }

        public void Error(object message, Exception exception)
        {
            RaiseError(exception ?? new System.ApplicationException(message?.ToString() ?? "(null)"));

            log.Error(message, exception);
        }

        public void Error(object message)
        {
            RaiseError(message);

            log.Error(message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            RaiseError(format, args);

            log.ErrorFormat(format, args);
        }

        public void Fatal(object message, Exception exception)
        {
            RaiseError(exception ?? new System.ApplicationException(message?.ToString() ?? "(null)"));

            log.Fatal(message, exception);
        }

        public void Fatal(object message)
        {
            RaiseError(message);

            log.Fatal(message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            RaiseError(format, args);

            log.FatalFormat(format, args);
        }

        public void Info(object message, Exception exception)
        {
            log.Info(message, exception);
        }

        public void Info(object message)
        {
            log.Info(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            log.InfoFormat(format, args);
        }

        public bool IsDebugEnabled => log.IsDebugEnabled;

        public void Warn(object message, Exception exception)
        {
            log.Warn(message, exception);
        }

        public void Warn(object message)
        {
            log.Warn(message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            log.WarnFormat(format, args);
        }
    }
}
