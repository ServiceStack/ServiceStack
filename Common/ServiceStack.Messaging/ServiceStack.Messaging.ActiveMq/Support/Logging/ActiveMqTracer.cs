using ServiceStack.Logging;

namespace ServiceStack.Messaging.ActiveMq.Support.Logging
{
    public class ActiveMqTracer : ActiveMQ.ITrace
    {
        private const string LOG_FORMAT = "ACTIVEMQ: {0}";
        protected ILog log;

        public ActiveMqTracer()
        {
            log = LogManager.GetLogger(GetType());
        }

        #region ITrace Members

        public void Debug(string message)
        {
            log.Debug(string.Format(LOG_FORMAT, message));
        }

        public void Info(string message)
        {
            log.Info(string.Format(LOG_FORMAT, message));
        }

        public void Warn(string message)
        {
            log.Warn(string.Format(LOG_FORMAT, message));
        }

        public void Error(string message)
        {
            //ActiveMQ log errors are not really errors unless they throw an exception
            log.Warn(message);
        }

        public void Fatal(object message)
        {
            log.Fatal(string.Format(LOG_FORMAT, message));
        }

        public bool IsDebugEnabled
        {
            get { return false; }
        }

        public bool IsInfoEnabled
        {
            get { return false; }
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

        #endregion
    }
}