using System;
using ServiceStack.Logging;

namespace ServiceStack.Caching.Memcached
{
    public class EnyimLoggerWarpper : Enyim.Caching.ILog
    {
        private readonly ILog _serviceStackLogger;

        public EnyimLoggerWarpper(ILog serviceStackLogger)
        {
            _serviceStackLogger = serviceStackLogger;
        }

        public void Debug(object message)
        {
            _serviceStackLogger.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            _serviceStackLogger.Debug(message, exception);
        }

        public void DebugFormat(string format, object arg0)
        {
            _serviceStackLogger.DebugFormat(format, arg0);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            _serviceStackLogger.DebugFormat(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _serviceStackLogger.DebugFormat(format, arg0, arg1, arg2);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _serviceStackLogger.DebugFormat(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _serviceStackLogger.DebugFormat(format, args);
        }

        public void Info(object message)
        {
            _serviceStackLogger.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            _serviceStackLogger.Info(message, exception);
        }

        public void InfoFormat(string format, object arg0)
        {
            _serviceStackLogger.InfoFormat(format, arg0);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            _serviceStackLogger.InfoFormat(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _serviceStackLogger.InfoFormat(format, arg0, arg1, arg2);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _serviceStackLogger.InfoFormat(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _serviceStackLogger.InfoFormat(format, args);
        }

        public void Warn(object message)
        {
            _serviceStackLogger.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            _serviceStackLogger.Warn(message, exception);
        }

        public void WarnFormat(string format, object arg0)
        {
            _serviceStackLogger.WarnFormat(format, arg0);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            _serviceStackLogger.WarnFormat(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _serviceStackLogger.WarnFormat(format, arg0, arg1, arg2);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _serviceStackLogger.WarnFormat(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _serviceStackLogger.WarnFormat(format, args);
        }

        public void Error(object message)
        {
            _serviceStackLogger.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            _serviceStackLogger.Error(message, exception);
        }

        public void ErrorFormat(string format, object arg0)
        {
            _serviceStackLogger.ErrorFormat(format, arg0);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _serviceStackLogger.ErrorFormat(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _serviceStackLogger.ErrorFormat(format, arg0, arg1, arg2);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _serviceStackLogger.ErrorFormat(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _serviceStackLogger.ErrorFormat(format, args);
        }

        public void Fatal(object message)
        {
            _serviceStackLogger.Fatal(message);
        }

        public void Fatal(object message, Exception exception)
        {
            _serviceStackLogger.Fatal(message, exception);
        }

        public void FatalFormat(string format, object arg0)
        {
            _serviceStackLogger.FatalFormat(format, arg0);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            _serviceStackLogger.FatalFormat(format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            _serviceStackLogger.FatalFormat(format, arg0, arg1, arg2);
        }

        public void FatalFormat(string format, params object[] args)
        {
            _serviceStackLogger.FatalFormat(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            _serviceStackLogger.FatalFormat(format, args);
        }

        public bool IsDebugEnabled
        {
            get { return _serviceStackLogger.IsDebugEnabled; }
        }

        public bool IsInfoEnabled
        {
            get { return true; }
        }

        public bool IsWarnEnabled
        {
            get { return true; }
        }

        public bool IsErrorEnabled
        {
            get { return true; }
        }

        public bool IsFatalEnabled
        {
            get { return true; }
        }
    }
}